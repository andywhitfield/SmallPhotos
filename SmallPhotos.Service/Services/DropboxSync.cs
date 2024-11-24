using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Dropbox;
using SmallPhotos.Model;

namespace SmallPhotos.Service.Services;

public class DropboxSync(
    ILogger<DropboxSync> logger,
    IDropboxClientProxy dropboxClientProxy,
    IAlbumRepository albumRepository,
    IPhotoRepository photoRepository
    ) : IDropboxSync
{
    public async Task SyncAsync(AlbumSource albumSource, UserAccount user, HttpClient httpClient)
    {
        logger.LogDebug("Checking Dropbox album changes for user [{UserAccountId}] / album [{AlbumSourceId}:{albumSourceFolder}]", user.UserAccountId, albumSource.AlbumSourceId, albumSource.Folder);

        dropboxClientProxy.Initialise(albumSource.DropboxAccessToken, albumSource.DropboxRefreshToken);
        if (!await dropboxClientProxy.RefreshAccessTokenAsync(["files.content.read"]))
        {
            logger.LogWarning("Could not refresh Dropbox access token");
            return;
        }

        if (await TrySyncUsingFolderCursorAsync(albumSource, user, httpClient))
            return;

        var dropboxFilesInAlbum = GetDropboxFilesForAlbumSourceAsync(albumSource);
        List<(string Filename, string RelativeFolder, DateTime LastWriteTime)> filesInAlbum = [];
        await foreach (var dropboxFile in dropboxFilesInAlbum)
            filesInAlbum.Add(dropboxFile);

        var photosInAlbum = await photoRepository.GetAllAsync(albumSource);

        logger.LogDebug("Files in folder: [{FilesInAlbum}]", string.Join(',', filesInAlbum.Select(fi => fi.Filename)));
        logger.LogDebug("Photos in album: [{PhotosInAlbum}]", string.Join(',', photosInAlbum.Select(p => p.Filename)));

        var newOrChangedPhotos = (
            from f in filesInAlbum
            join p in photosInAlbum on new { Filename = f.Filename, RelativePath = f.RelativeFolder } equals new { p.Filename, RelativePath = string.IsNullOrEmpty(p.RelativePath) ? "" : p.RelativePath } into j
            from m in j.DefaultIfEmpty()
            where m == null || m.FileModificationDateTime < f.LastWriteTime
            orderby f.LastWriteTime descending
            select f).ToList();

        logger.LogInformation("New or changed photos in album: [{NewOrChangedPhotos}]", string.Join(',', newOrChangedPhotos.Select(f => f.Filename)));

        if (newOrChangedPhotos.Count != 0)
            logger.LogTrace("Using temporary download directory: {TemporaryDownloadDirectory}", dropboxClientProxy.TemporaryDownloadDirectory.FullName);

        foreach (var (filename, relativeFolder, lastWriteTime) in newOrChangedPhotos)
            await AddOrUpdatePhotoAsync(dropboxClientProxy, albumSource, user, httpClient, filename, relativeFolder);

        var deletedPhotos = (
            from p in photosInAlbum
            join f in filesInAlbum on p.Filename equals f.Filename into j
            from m in j.DefaultIfEmpty()
            where m.Filename == null
            select p
        ).ToList();

        logger.LogInformation("Deleting photos in album: [{DeletedPhotos}]", string.Join(',', deletedPhotos.Select(p => p.Filename)));
        await deletedPhotos.DeletePhotosAsync(photoRepository);
    }

    private async Task AddOrUpdatePhotoAsync(IDropboxClientProxy dropboxClientProxy, AlbumSource albumSource,
        UserAccount user, HttpClient httpClient, string filename, string relativeFolder)
    {
        logger.LogDebug("Adding / updating photo {Filename}|{RelativeFolder}", filename, relativeFolder);
        var localFile = await DownloadFileFromDropboxAsync(dropboxClientProxy.TemporaryDownloadDirectory, albumSource.Folder ?? "/", relativeFolder, filename);
        try
        {
            logger.LogTrace("Adding / updating photo {LocalFile} in folder {RelativeFolder}", localFile, relativeFolder);
            var responseString = await httpClient.PostCreateOrUpdatePhotoAsync(user, albumSource, localFile, relativeFolder);
            logger.LogInformation("Successfully updated / added new photo: {ResponseString}", responseString);
        }
        finally
        {
            try
            {
                logger.LogTrace("Deleting temporary file: {LocalFile}", localFile);
                File.Delete(localFile);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cannot delete temporary file downloaded from dropbox [{LocalFile}]", localFile);
            }
        }
    }

    private async IAsyncEnumerable<(string Filename, string RelativeFolder, DateTime LastWriteTime)> GetDropboxFilesForAlbumSourceAsync(AlbumSource albumSource)
    {
        var files = await dropboxClientProxy.ListFolderAsync(albumSource.Folder, albumSource.RecurseSubFolders ?? false);
        while (files != null)
        {
            foreach (var file in files.Entries)
            {
                logger.LogTrace("Got Dropbox entry: {IsFile} | {PathLower} | {FileName}", file.IsFile, file.PathLower, file.Name);
                if (file.IsDeleted || !file.IsFile || !SyncExtensions.SupportedPhotoExtensions.Contains(Path.GetExtension(file.Name.ToLowerInvariant())))
                {
                    logger.LogTrace("Entry {FilePathLower} is deleted [{FileIsDeleted}], not a file [{NotFileIsFile}], or is not an image [ext={FileExt}], skipping", file.PathLower, file.IsDeleted, !file.IsFile, Path.GetExtension(file.Name.ToLowerInvariant()));
                    continue;
                }

                yield return (file.Name, file.PathLower.GetRelativePath(albumSource.Folder, file.Name), file.AsFile.ServerModified);
            }

            await UpdateDropboxCursorAsync(albumSource, files.Cursor);

            if (files.HasMore)
            {
                logger.LogTrace("More entries, calling list folder for cursor: {FilesCursor}", files.Cursor);
                files = await dropboxClientProxy.ListFolderContinueAsync(files.Cursor);
            }
            else
            {
                break;
            }
        }
    }

    private async Task<string> DownloadFileFromDropboxAsync(DirectoryInfo downloadTmpDir, string baseDir, string relativeFolder, string filename)
    {
        logger.LogDebug("Downloading file [{Filename}] from Dropbox to [{DownloadTmpDirFullName}] sub-folder [{RelativeFolder}]", filename, downloadTmpDir.FullName, relativeFolder);

        var localFilename = ModelExtensions.CombinePath(downloadTmpDir.FullName, relativeFolder, filename);
        logger.LogTrace("Downloading to temporary file: {LocalFilename}", localFilename);
        var localFileDir = Directory.GetParent(localFilename)?.FullName ?? "";
        if (!Directory.Exists(localFileDir))
            Directory.CreateDirectory(localFileDir);

        var remoteFilename = ModelExtensions.GetDropboxPhotoPath(baseDir, relativeFolder, filename);
        logger.LogTrace("Downlading from Dropbox: {RemoteFilename}", remoteFilename);
        var downloadedFile = await dropboxClientProxy.DownloadAsync(remoteFilename);
        if (downloadedFile == null)
            throw new ArgumentException($"Cannot download file {remoteFilename}");

        await using FileStream imgFile = new(localFilename, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await (await downloadedFile.GetContentAsStreamAsync()).CopyToAsync(imgFile);

        logger.LogDebug("Setting file [{LocalFilename}] time to {DownloadedFileResponseServerModified}", localFilename, downloadedFile.Response.ServerModified);

        File.SetCreationTimeUtc(localFilename, downloadedFile.Response.ServerModified);
        File.SetLastWriteTimeUtc(localFilename, downloadedFile.Response.ServerModified);

        return localFilename;
    }

    private async Task<bool> TrySyncUsingFolderCursorAsync(AlbumSource albumSource, UserAccount user, HttpClient httpClient)
    {
        if (string.IsNullOrEmpty(albumSource.DropboxCursor))
            return false;

        try
        {
            while (true)
            {
                var fileChanges = await dropboxClientProxy.ListFolderContinueAsync(albumSource.DropboxCursor);
                if (fileChanges == null)
                {
                    logger.LogWarning("Folder listing for cursor [{DropboxCursor}] returned null, will assume the cursor is invalid, and clear it.", albumSource.DropboxCursor);
                    await UpdateDropboxCursorAsync(albumSource, null);
                    return false;
                }

                foreach (var file in fileChanges.Entries)
                {
                    logger.LogTrace("Got Dropbox entry: {IsFile} | {PathLower} | {FileName} | {IsDeleted}", file.IsFile, file.PathLower, file.Name, file.IsDeleted);
                    if (!file.IsFile || !SyncExtensions.SupportedPhotoExtensions.Contains(Path.GetExtension(file.Name.ToLowerInvariant())))
                    {
                        logger.LogTrace("Entry {FilePathLower} is not a file [{NotFileIsFile}], or is not an image [ext={FileExt}], skipping", file.PathLower, !file.IsFile, Path.GetExtension(file.Name.ToLowerInvariant()));
                        continue;
                    }

                    Photo? photo;
                    if (file.IsDeleted && (photo = await photoRepository.GetAsync(user, albumSource, file.Name, file.PathLower.GetRelativePath(albumSource.Folder, file.Name))) != null)                        
                        await photoRepository.DeleteAsync(photo);
                    else
                        await AddOrUpdatePhotoAsync(dropboxClientProxy, albumSource, user, httpClient, file.Name, file.PathLower.GetRelativePath(albumSource.Folder, file.Name));

                    logger.LogInformation("New Dropbox change [{FileName}] successfully processed", file.Name);
                }

                await UpdateDropboxCursorAsync(albumSource, fileChanges.Cursor);
                logger.LogInformation("Successfully processed updates from Dropbox for cursor: {Cursor}", fileChanges.Cursor);

                if (!fileChanges.HasMore)
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not sync using cursor [{DropboxCursor}], will assume the cursor is invalid, and clear it.", albumSource.DropboxCursor);
            await UpdateDropboxCursorAsync(albumSource, null);
            return false;
        }

        return true;
    }

    private async Task UpdateDropboxCursorAsync(AlbumSource albumSource, string? cursor)
    {
        if (albumSource.DropboxCursor != cursor)
        {
            albumSource.DropboxCursor = cursor;
            await albumRepository.UpdateAsync(albumSource);
        }
    }
}