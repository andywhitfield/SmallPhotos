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
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Service.BackgroundServices;
using SmallPhotos.Service.Models;

namespace SmallPhotos.Service.Services;

public class FilesystemSync : IFilesystemSync
{
    private readonly ILogger<FilesystemSync> _logger;
    private readonly IOptionsSnapshot<AlbumChangeServiceOptions> _options;
    private readonly IPhotoRepository _photoRepository;

    public FilesystemSync(
        ILogger<FilesystemSync> logger,
        IOptionsSnapshot<AlbumChangeServiceOptions> options,
        IPhotoRepository photoRepository
    )
    {
        _logger = logger;
        _options = options;
        _photoRepository = photoRepository;
    }

    public async Task SyncAsync(AlbumSource albumSource, UserAccount user, HttpClient httpClient)
    {
        _logger.LogDebug($"Checking album changes for user [{user.UserAccountId}] / album [{albumSource.AlbumSourceId}:{albumSource.Folder}]");

        var filesInAlbum = GetFilesForAlbumSource(albumSource);

        var photosInAlbum = await _photoRepository.GetAllAsync(albumSource);

        _logger.LogDebug($"Files in folder: [{string.Join(',', filesInAlbum.Select(fi => fi.Name))}]");
        _logger.LogDebug($"Photos in album: [{string.Join(',', photosInAlbum.Select(p => p.Filename))}]");

        var newOrChangedPhotos = (
            from f in filesInAlbum
            join p in photosInAlbum on new { Filename = f.Name, RelativePath = albumSource.Folder.GetRelativePath(f) } equals new { p.Filename, RelativePath = string.IsNullOrEmpty(p.RelativePath) ? "" : p.RelativePath } into j
            from m in j.DefaultIfEmpty()
            where m == null || m.FileModificationDateTime < f.LastWriteTimeUtc
            orderby f.LastWriteTimeUtc descending
            select f).ToList();

        _logger.LogInformation($"New or changed photos in album: [{string.Join(',', newOrChangedPhotos.Select(fi => fi.Name))}]");

        foreach (var requestBatch in newOrChangedPhotos.Chunk(_options.Value.SyncPhotoBatchSize))
        {
            await Task.WhenAll(requestBatch.Select(async newOrChanged =>
            {
                using var response = await httpClient.PostAsync("/api/photo", new StringContent(JsonSerializer.Serialize(
                    new CreateOrUpdatePhotoRequest { UserAccountId = user.UserAccountId, AlbumSourceId = albumSource.AlbumSourceId, Filename = newOrChanged.Name, FilePath = albumSource.Folder.GetRelativePath(newOrChanged) }),
                    Encoding.UTF8,
                    "application/json"));

                var responseString = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Could not add/update photo [{newOrChanged.Name}] in album [{albumSource.AlbumSourceId}]: {responseString}");

                _logger.LogInformation($"Successfully updated / added new photo: {responseString}");
            }));
        }

        var deletedPhotos = (
            from p in photosInAlbum
            join f in filesInAlbum on p.Filename equals f.Name into j
            from m in j.DefaultIfEmpty()
            where m == null
            select p
        ).ToList();

        _logger.LogInformation($"Deleting photos in album: [{string.Join(',', deletedPhotos.Select(p => p.Filename))}]");
        foreach (var photo in deletedPhotos)
            await _photoRepository.DeleteAsync(photo);
    }

    private IEnumerable<FileInfo> GetFilesForAlbumSource(AlbumSource albumSource) =>
        GetPhotoFilesInDirectory(new(albumSource.Folder ?? ""), albumSource.RecurseSubFolders ?? false);

    private IEnumerable<FileInfo> GetPhotoFilesInDirectory(DirectoryInfo dir, bool recurse)
    {
        if (!dir.Exists)
            yield break;

        foreach (var file in dir.EnumerateFiles().Where(f => SyncExtensions.SupportedPhotoExtensions.Contains(f.Extension.ToLowerInvariant())))
            yield return file;

        if (recurse)
        {
            foreach (var subDir in dir.EnumerateDirectories())
                foreach (var file in GetPhotoFilesInDirectory(subDir, recurse))
                    yield return file;
        }
    }
}