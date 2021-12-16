using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
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

        public GetPhotoRequestHandler(ILogger<GetPhotoRequestHandler> logger, IUserAccountRepository userAccountRepository,
            IPhotoRepository photoRepository)
        {
            _logger = logger;
            _userAccountRepository = userAccountRepository;
            _photoRepository = photoRepository;
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

            ThumbnailSize thumbnailSize;
            if (string.IsNullOrEmpty(request.ThumbnailSize))
                thumbnailSize = ThumbnailSize.Medium;
            else if (!Enum.TryParse<ThumbnailSize>(request.ThumbnailSize, true, out thumbnailSize))
            {
                _logger.LogInformation($"Unknown thumbnail size [{request.ThumbnailSize}]");
                return GetPhotoResponse.Empty;
            }

            // TODO: this should have been created by the background process, but if not, we should
            //       create the thumbnail & save...but probably needs to be a separate (shared) service
            var thumbnail = await _photoRepository.GetThumbnailAsync(photo, thumbnailSize);

            var jpegStream = thumbnail == null ? new MemoryStream() : new MemoryStream(thumbnail.ThumbnailImage);
            if (thumbnail == null)
            {
                using (var image = new MagickImage(Path.Combine(photo.AlbumSource.Folder, photo.Filename), MagickFormat.Heic))
                {
                    var resizeTo = thumbnailSize.ToSize();
                    image.Sample(resizeTo.Width, resizeTo.Height);
                    await image.WriteAsync(jpegStream, MagickFormat.Jpeg);
                }

                await _photoRepository.SaveThumbnailAsync(photo, thumbnailSize, jpegStream.ToArray());

                jpegStream.Position = 0;
            }

            return new GetPhotoResponse(jpegStream);
        }
    }
}