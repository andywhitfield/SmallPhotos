using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Service.BackgroundServices;

namespace SmallPhotos.Service.Services;

public class FilesystemSync(
    ILogger<FilesystemSync> logger,
    IOptionsSnapshot<AlbumChangeServiceOptions> options,
    IPhotoRepository photoRepository
    ) : IFilesystemSync
{
    public async Task SyncAsync(AlbumSource albumSource, UserAccount user, HttpClient httpClient)
    {
        logger.LogDebug("Checking album changes for user [{UserAccountId}] / album [{AlbumSourceId}:{AlbumSourceFolder}]", user.UserAccountId, albumSource.AlbumSourceId, albumSource.Folder);

        var filesInAlbum = GetFilesForAlbumSource(albumSource);
        var photosInAlbum = await photoRepository.GetAllAsync(albumSource);

        logger.LogDebug("Files in folder: [{FilesInAlbum}]", string.Join(',', filesInAlbum.Select(fi => fi.Name)));
        logger.LogDebug("Photos in album: [{PhotosInAlbum}]", string.Join(',', photosInAlbum.Select(p => p.Filename)));

        var newOrChangedPhotos = (
            from f in filesInAlbum
            join p in photosInAlbum on new { Filename = f.Name, RelativePath = albumSource.Folder.GetRelativePath(f) } equals new { p.Filename, RelativePath = string.IsNullOrEmpty(p.RelativePath) ? "" : p.RelativePath } into j
            from m in j.DefaultIfEmpty()
            where m == null || m.FileModificationDateTime < f.LastWriteTimeUtc
            orderby f.LastWriteTimeUtc descending
            select f).ToList();

        logger.LogInformation("New or changed photos in album: [{NewOrChangedPhotos}]", string.Join(',', newOrChangedPhotos.Select(fi => fi.Name)));

        foreach (var requestBatch in newOrChangedPhotos.Chunk(options.Value.SyncPhotoBatchSize))
        {
            await Task.WhenAll(requestBatch.Select(async newOrChanged =>
            {
                var responseString = await httpClient.PostCreateOrUpdatePhotoAsync(user, albumSource, newOrChanged.Name, albumSource.Folder.GetRelativePath(newOrChanged));
                logger.LogInformation("Successfully updated / added new photo: {ResponseString}", responseString);
            }));
        }

        var deletedPhotos = (
            from p in photosInAlbum
            join f in filesInAlbum on p.Filename equals f.Name into j
            from m in j.DefaultIfEmpty()
            where m == null
            select p
        ).ToList();

        logger.LogInformation("Deleting photos in album: [{deletedPhotos}]", string.Join(',', deletedPhotos.Select(p => p.Filename)));
        await deletedPhotos.DeletePhotosAsync(photoRepository);
    }

    private static IEnumerable<FileInfo> GetFilesForAlbumSource(AlbumSource albumSource) =>
        GetPhotoFilesInDirectory(new(albumSource.Folder ?? ""), albumSource.RecurseSubFolders ?? false);

    private static IEnumerable<FileInfo> GetPhotoFilesInDirectory(DirectoryInfo dir, bool recurse)
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