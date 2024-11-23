using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Dropbox;
using SmallPhotos.Model;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers;

public class GetPhotoRequestHandler(
    ILogger<GetPhotoRequestHandler> logger,
    IUserAccountRepository userAccountRepository,
    IPhotoRepository photoRepository,
    IPhotoReader photoReader,
    IDropboxClientProxy dropboxClientProxy)
    : IRequestHandler<GetPhotoRequest, GetPhotoResponse>
{
    public async Task<GetPhotoResponse> Handle(GetPhotoRequest request, CancellationToken cancellationToken)
    {
        var user = await userAccountRepository.GetUserAccountAsync(request.User);
        var photo = await photoRepository.GetAsync(user, request.PhotoId);
        if (photo == null)
        {
            logger.LogInformation("No photo with id {RequestPhotoId}", request.PhotoId);
            return GetPhotoResponse.Empty;
        }

        if (!string.Equals(photo.Filename, request.Name, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Found photo with id {RequestPhotoId}, but names don't match [{PhotoFilename}] vs [{RequestName}]", request.PhotoId, photo.Filename, request.Name);
            return GetPhotoResponse.Empty;
        }

        if (request.ThumbnailSize == null)
        {
            var original = await LoadPhotoFileAsync(photo, request.Original);
            if (!(original?.Exists ?? false))
            {
                logger.LogInformation("Photo with id {RequestPhotoId} does not exist: [{OriginalFullName}]", request.PhotoId, original?.FullName);
                return GetPhotoResponse.Empty;
            }

            var (contentType, stream) = await photoReader.GetPhotoStreamForWebAsync(original);

            if (contentType == null)
            {
                logger.LogInformation("Photo [{RequestPhotoId} / {OriginalName}] cannot be mapped to a known content type", request.PhotoId, original.Name);
                return GetPhotoResponse.Empty;
            }

            return new(stream, contentType, ImageUpdatedDate(photo), GenerateETag(stream));
        }

        ThumbnailSize thumbnailSize;
        if (!Enum.TryParse<ThumbnailSize>(request.ThumbnailSize, true, out thumbnailSize))
        {
            logger.LogInformation("Unknown thumbnail size [{RequestThumbnailSize}]", request.ThumbnailSize);
            return GetPhotoResponse.Empty;
        }

        var thumbnail = await photoRepository.GetThumbnailAsync(photo, thumbnailSize);
        if (thumbnail?.ThumbnailImage == null)
        {
            logger.LogInformation("No thumbnail for photo [{RequestPhotoId}]", request.PhotoId);
            return GetPhotoResponse.Empty;
        }

        var imgStream = new MemoryStream(thumbnail.ThumbnailImage);
        return new(imgStream, "image/jpeg", ImageUpdatedDate(photo), GenerateETag(imgStream));
    }

    private static DateTime ImageUpdatedDate(Photo photo) => photo.LastUpdateDateTime ?? photo.FileModificationDateTime;

    private static string? GenerateETag(Stream? stream)
    {
        if (stream == null)
            return null;
        
        var checksum = MD5.Create().ComputeHash(stream);
        stream.Position = 0;
        return Convert.ToBase64String(checksum, 0, checksum.Length);
    }

    private async Task<FileInfo?> LoadPhotoFileAsync(Photo photo, bool original)
    {
        if (photo.AlbumSource!.IsDropboxSource)
        {
            if (original)
                return await LoadPhotoFileFromDropboxAsync(photo);
            else
                return await LoadLargeThumbnailAsync(photo);
        }

        return new(photo.AlbumSource!.PhotoPath(photo.RelativePath, photo.Filename ?? ""));
    }

    private async Task<FileInfo?> LoadPhotoFileFromDropboxAsync(Photo photo)
    {
        var dropboxFilename = ModelExtensions.GetDropboxPhotoPath(photo.AlbumSource!.Folder, photo.RelativePath, photo.Filename);

        logger.LogTrace("Loading photo file (photo={PhotoId} folder=[{AlbumSourceFolder}] photo path=[{PhotoRelativePath}] name=[{PhotoFilename}]) from Dropbox: {DropboxFilename}", photo.PhotoId, photo.AlbumSource.Folder, photo.RelativePath, photo.Filename, dropboxFilename);

        try
        {
            dropboxClientProxy.Initialise(photo.AlbumSource.DropboxAccessToken, photo.AlbumSource.DropboxRefreshToken);

            var localFilename = Path.Combine(dropboxClientProxy.TemporaryDownloadDirectory.FullName, Path.GetRandomFileName() + Path.GetExtension(photo.Filename));
            logger.LogTrace("Downloading from Dropbox: {DropboxFilename}", dropboxFilename);
            var downloadResponse = await dropboxClientProxy.DownloadAsync(dropboxFilename);
            if (downloadResponse == null)
                throw new InvalidOperationException($"Cannot download file from Dropbox {dropboxFilename}");

            logger.LogTrace("Downloaded from Dropbox, returning to client");
            await using FileStream imgFile = new(localFilename, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await (await downloadResponse.GetContentAsStreamAsync()).CopyToAsync(imgFile);

            return new(localFilename);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not download photo [{PhotoId}] file [{DropboxFilename}] from Dropbox", photo.PhotoId, dropboxFilename);
            return null;
        }
    }

    private async Task<FileInfo?> LoadLargeThumbnailAsync(Photo photo)
    {
        logger.LogTrace("Photo {PhotoId} [{Filename}] is on Dropbox...showing large thumbnail", photo.PhotoId, photo.Filename);
        var largeThumbnail = await photoRepository.GetThumbnailAsync(photo, ThumbnailSize.Large);
        if (largeThumbnail?.ThumbnailImage == null)
        {
            logger.LogWarning("Could not find large thumbnail for photo {PhotoId} [{Filename}]", photo.PhotoId, photo.Filename);
            return null;
        }

        var localFilename = Path.Combine(dropboxClientProxy.TemporaryDownloadDirectory.FullName, Path.GetRandomFileName() + Path.GetExtension(photo.Filename));
        await File.WriteAllBytesAsync(localFilename, largeThumbnail.ThumbnailImage);

        return new(localFilename);
    }
}