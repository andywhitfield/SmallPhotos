using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Service.BackgroundServices;
using SmallPhotos.Service.Models;

namespace SmallPhotos.Service.Services;

public class DropboxSync : IDropboxSync
{
    private readonly ILogger<DropboxSync> _logger;
    private readonly IDropboxClientProxy _dropboxClientProxy;
    private readonly IOptionsSnapshot<AlbumChangeServiceOptions> _options;
    private readonly IOptionsSnapshot<DropboxOptions> _dropboxOptions;
    private readonly IPhotoRepository _photoRepository;

    public DropboxSync(
        ILogger<DropboxSync> logger,
        IDropboxClientProxy dropboxClientProxy,
        IOptionsSnapshot<AlbumChangeServiceOptions> options,
        IOptionsSnapshot<DropboxOptions> dropboxOptions,
        IPhotoRepository photoRepository
    )
    {
        _logger = logger;
        _dropboxClientProxy = dropboxClientProxy;
        _options = options;
        _dropboxOptions = dropboxOptions;
        _photoRepository = photoRepository;
    }

    public async Task SyncAsync(AlbumSource albumSource, UserAccount user, HttpClient httpClient)
    {
        _logger.LogDebug($"Checking Dropbox album changes for user [{user.UserAccountId}] / album [{albumSource.AlbumSourceId}:{albumSource.Folder}]");

        _dropboxClientProxy.Initialise(albumSource.DropboxAccessToken, albumSource.DropboxRefreshToken);
        if (!await _dropboxClientProxy.RefreshAccessTokenAsync(new[] { "files.content.read" }))
        {
            _logger.LogWarning($"Could not refresh Dropbox access token");
            return;
        }

        var dropboxFilesInAlbum = GetDropboxFilesForAlbumSourceAsync(albumSource);
        List<(string Filename, string RelativeFolder, DateTime LastWriteTime)> filesInAlbum = new();
        await foreach (var dropboxFile in dropboxFilesInAlbum)
            filesInAlbum.Add(dropboxFile);

        var photosInAlbum = await _photoRepository.GetAllAsync(albumSource);

        _logger.LogDebug($"Files in folder: [{string.Join(',', filesInAlbum.Select(fi => fi.Filename))}]");
        _logger.LogDebug($"Photos in album: [{string.Join(',', photosInAlbum.Select(p => p.Filename))}]");

        var newOrChangedPhotos = (
            from f in filesInAlbum
            join p in photosInAlbum on new { Filename = f.Filename, RelativePath = f.RelativeFolder } equals new { p.Filename, RelativePath = string.IsNullOrEmpty(p.RelativePath) ? "" : p.RelativePath } into j
            from m in j.DefaultIfEmpty()
            where m == null || m.FileModificationDateTime < f.LastWriteTime
            orderby f.LastWriteTime descending
            select f).ToList();

        _logger.LogInformation($"New or changed photos in album: [{string.Join(',', newOrChangedPhotos.Select(f => f.Filename))}]");

        var downloadTmpDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        _logger.LogTrace($"Created temporary download directory: {downloadTmpDir.FullName}");

        foreach (var requestBatch in newOrChangedPhotos.Chunk(_options.Value.SyncPhotoBatchSize))
        {
            await Task.WhenAll(requestBatch.Select(async newOrChanged =>
            {
                var localFile = await DownloadFileFromDropboxAsync(downloadTmpDir, albumSource.Folder ?? "/", newOrChanged.RelativeFolder, newOrChanged.Filename);
                try
                {
                    _logger.LogTrace($"Adding / updating photo {localFile} in folder {newOrChanged.RelativeFolder}");

                    using var response = await httpClient.PostAsync("/api/photo", new StringContent(JsonSerializer.Serialize(
                        new CreateOrUpdatePhotoRequest { UserAccountId = user.UserAccountId, AlbumSourceId = albumSource.AlbumSourceId, Filename = localFile, FilePath = newOrChanged.RelativeFolder }),
                        Encoding.UTF8,
                        "application/json"));

                    var responseString = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                        throw new InvalidOperationException($"Could not add/update photo [{newOrChanged.Filename}] in album [{albumSource.AlbumSourceId}]: {responseString}");

                    _logger.LogInformation($"Successfully updated / added new photo: {responseString}");
                }
                finally
                {
                    try
                    {
                        _logger.LogTrace($"Deleting temporary file: {localFile}");
                        File.Delete(localFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Cannot delete temporary file downloaded from dropbox [{localFile}]");
                    }
                }
            }));
        }

        var deletedPhotos = (
            from p in photosInAlbum
            join f in filesInAlbum on p.Filename equals f.Filename into j
            from m in j.DefaultIfEmpty()
            where m.Filename == null
            select p
        ).ToList();

        _logger.LogInformation($"Deleting photos in album: [{string.Join(',', deletedPhotos.Select(p => p.Filename))}]");
        foreach (var photo in deletedPhotos)
            await _photoRepository.DeleteAsync(photo);

        _logger.LogTrace($"Deleting temporary Dropbox download directory: {downloadTmpDir.FullName}");
        Policy
            .Handle<Exception>()
            .WaitAndRetry(3, retry => TimeSpan.FromSeconds(retry), (ex, ts) => _logger.LogWarning(ex, $"Error deleting temporary download directory [{downloadTmpDir.FullName}], trying again in {ts}"))
            .Execute(() => downloadTmpDir.Delete(true));
    }

    private async IAsyncEnumerable<(string Filename, string RelativeFolder, DateTime LastWriteTime)> GetDropboxFilesForAlbumSourceAsync(AlbumSource albumSource)
    {
        var files = await _dropboxClientProxy.ListFolderAsync(albumSource.Folder, albumSource.RecurseSubFolders ?? false);
        while (true)
        {
            foreach (var file in files.Entries)
            {
                _logger.LogTrace($"Got Dropbox entry: {file.IsFile} | {file.PathLower} | {file.Name}");
                if (file.IsDeleted || !file.IsFile || !SyncExtensions.SupportedPhotoExtensions.Contains(Path.GetExtension(file.Name.ToLowerInvariant())))
                {
                    _logger.LogTrace($"Entry {file.PathLower} is deleted [{file.IsDeleted}], not a file [{!file.IsFile}], or is not an image [ext={Path.GetExtension(file.Name.ToLowerInvariant())}], skipping");
                    continue;
                }

                yield return (file.Name, file.PathLower.GetRelativePath(albumSource.Folder, file.Name), file.AsFile.ServerModified);
            }

            if (files.HasMore)
            {
                _logger.LogTrace($"More entries, calling list folder for cursor: {files.Cursor}");
                files = await _dropboxClientProxy.ListFolderContinueAsync(files.Cursor);
            }
            else
                break;
        }
    }

    private async Task<string> DownloadFileFromDropboxAsync(DirectoryInfo downloadTmpDir, string baseDir, string relativeFolder, string filename)
    {
        _logger.LogDebug($"Downloading file [{filename}] from Dropbox to [{downloadTmpDir.FullName}] sub-folder [{relativeFolder}]");

        var localFilename = Path.Combine(downloadTmpDir.FullName, relativeFolder, filename);
        var downloadedFile = await _dropboxClientProxy.DownloadAsync($"{baseDir}/{(string.IsNullOrEmpty(relativeFolder) ? "" : $"{relativeFolder}/")}{filename}");
        await using FileStream imgFile = new(localFilename, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await (await downloadedFile.GetContentAsStreamAsync()).CopyToAsync(imgFile);

        _logger.LogDebug($"Setting file [{localFilename}] time to {downloadedFile.Response.ServerModified}");

        File.SetCreationTimeUtc(localFilename, downloadedFile.Response.ServerModified);
        File.SetLastWriteTimeUtc(localFilename, downloadedFile.Response.ServerModified);

        return localFilename;
    }
}