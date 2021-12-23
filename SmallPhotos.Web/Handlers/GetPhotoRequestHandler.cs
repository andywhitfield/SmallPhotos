using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers
{
    public class GetPhotoRequestHandler : IRequestHandler<GetPhotoRequest, GetPhotoResponse>
    {
        private readonly ILogger<GetPhotoRequestHandler> _logger;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IPhotoRepository _photoRepository;
        private readonly IPhotoReader _photoReader;

        public GetPhotoRequestHandler(
            ILogger<GetPhotoRequestHandler> logger,
            IUserAccountRepository userAccountRepository,
            IPhotoRepository photoRepository,
            IPhotoReader photoReader)
        {
            _logger = logger;
            _userAccountRepository = userAccountRepository;
            _photoRepository = photoRepository;
            _photoReader = photoReader;
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
                var original = new FileInfo(Path.Combine(photo.AlbumSource.Folder, photo.Filename));
                if (!original.Exists)
                {
                    _logger.LogInformation($"Photo with id {request.PhotoId} does not exist: [{original.FullName}]");
                    return GetPhotoResponse.Empty;
                }

                var (contentType, stream) = await _photoReader.GetPhotoStreamForWebAsync(original);
                
                if (contentType == null)
                {
                    _logger.LogInformation($"Photo [{request.PhotoId} / {original.Name}] cannot be mapped to a known content type");
                    return GetPhotoResponse.Empty;
                }

                return new GetPhotoResponse(stream, contentType);
            }

            ThumbnailSize thumbnailSize;
            if (!Enum.TryParse<ThumbnailSize>(request.ThumbnailSize, true, out thumbnailSize))
            {
                _logger.LogInformation($"Unknown thumbnail size [{request.ThumbnailSize}]");
                return GetPhotoResponse.Empty;
            }

            var thumbnail = await _photoRepository.GetThumbnailAsync(photo, thumbnailSize);
            if (thumbnail == null)
            {
                _logger.LogInformation($"No thumbnail for photo [{request.PhotoId}]");
                return GetPhotoResponse.Empty;
            }

            return new GetPhotoResponse(new MemoryStream(thumbnail.ThumbnailImage), "image/jpeg");
        }
    }
}