using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Dropbox;
using SmallPhotos.Model;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers;

public class GetPhotoRequestHandler : IRequestHandler<GetPhotoRequest, GetPhotoResponse>
{
    private readonly ILogger<GetPhotoRequestHandler> _logger;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IPhotoRepository _photoRepository;
    private readonly IPhotoReader _photoReader;
    private readonly IDropboxClientProxy _dropboxClientProxy;

    public GetPhotoRequestHandler(
        ILogger<GetPhotoRequestHandler> logger,
        IUserAccountRepository userAccountRepository,
        IPhotoRepository photoRepository,
        IPhotoReader photoReader,
        IDropboxClientProxy dropboxClientProxy)
    {
        _logger = logger;
        _userAccountRepository = userAccountRepository;
        _photoRepository = photoRepository;
        _photoReader = photoReader;
        _dropboxClientProxy = dropboxClientProxy;
    }

    public async Task<GetPhotoResponse> Handle(GetPhotoRequest request, CancellationToken cancellationToken)
    {
        var user = await _userAccountRepository.GetUserAccountAsync(request.User);
        var photo = await _photoRepository.GetAsync(user, request.PhotoId);
        if (photo == null)
        {
            _logger.LogInformation($"No photo with id {request.PhotoId}");
            return GetPhotoResponse.Empty;
        }

        if (!string.Equals(photo.Filename, request.Name, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation($"Found photo with id {request.PhotoId}, but names don't match [{photo.Filename}] vs [{request.Name}]");
            return GetPhotoResponse.Empty;
        }

        if (request.ThumbnailSize == null)
        {
            var original = await LoadPhotoFileAsync(photo);
            if (!(original?.Exists ?? false))
            {
                _logger.LogInformation($"Photo with id {request.PhotoId} does not exist: [{original?.FullName}]");
                return GetPhotoResponse.Empty;
            }

            var (contentType, stream) = await _photoReader.GetPhotoStreamForWebAsync(original);

            if (contentType == null)
            {
                _logger.LogInformation($"Photo [{request.PhotoId} / {original.Name}] cannot be mapped to a known content type");
                return GetPhotoResponse.Empty;
            }

            return new(stream, contentType);
        }

        ThumbnailSize thumbnailSize;
        if (!Enum.TryParse<ThumbnailSize>(request.ThumbnailSize, true, out thumbnailSize))
        {
            _logger.LogInformation($"Unknown thumbnail size [{request.ThumbnailSize}]");
            return GetPhotoResponse.Empty;
        }

        var thumbnail = await _photoRepository.GetThumbnailAsync(photo, thumbnailSize);
        if (thumbnail?.ThumbnailImage == null)
        {
            _logger.LogInformation($"No thumbnail for photo [{request.PhotoId}]");
            return GetPhotoResponse.Empty;
        }

        return new(new MemoryStream(thumbnail.ThumbnailImage), "image/jpeg");
    }

    private async Task<FileInfo?> LoadPhotoFileAsync(Photo photo)
    {
        if (photo.AlbumSource!.IsDropboxSource)
        {
            StringBuilder dropboxFilename = new(photo.AlbumSource.Folder ?? "");
            AppendDirectorySeparator(dropboxFilename);

            if (!string.IsNullOrEmpty(photo.RelativePath))
                dropboxFilename.Append(photo.RelativePath);

            AppendDirectorySeparator(dropboxFilename)
                .Append(photo.Filename);

            _logger.LogTrace($"Loading photo file (photo={photo.PhotoId} folder=[{photo.AlbumSource.Folder}] photo path=[{photo.RelativePath}] name=[{photo.Filename}]) from Dropbox: {dropboxFilename}");

            try
            {
                _dropboxClientProxy.Initialise(photo.AlbumSource.DropboxAccessToken, photo.AlbumSource.DropboxRefreshToken);

                var localFilename = Path.Combine(_dropboxClientProxy.TemporaryDownloadDirectory.FullName, Path.GetRandomFileName() + Path.GetExtension(photo.Filename));
                _logger.LogTrace($"Downloading from Dropbox: {dropboxFilename}");
                var downloadResponse = await _dropboxClientProxy.DownloadAsync(dropboxFilename.ToString());

                _logger.LogTrace("Downloaded from Dropbox, returning to client");
                await using FileStream imgFile = new(localFilename, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await (await downloadResponse.GetContentAsStreamAsync()).CopyToAsync(imgFile);

                return new(localFilename);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not download photo [{photo.PhotoId}] file [{dropboxFilename}] from Dropbox");
                return null;
            }
        }

        return new(photo.AlbumSource!.PhotoPath(photo.RelativePath, photo.Filename ?? ""));

        static StringBuilder AppendDirectorySeparator(StringBuilder path)
        {
            if (path.Length == 0 || path[path.Length - 1] != '/')
                path.Append('/');
            return path;
        }
    }
}