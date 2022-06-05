using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers
{
    public class GalleryRequestHandler : IRequestHandler<GalleryRequest, GalleryResponse>
    {
        private readonly ILogger<GalleryRequestHandler> _logger;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IPhotoRepository _photoRepository;

        public GalleryRequestHandler(ILogger<GalleryRequestHandler> logger, IUserAccountRepository userAccountRepository,
            IPhotoRepository photoRepository)
        {
            _logger = logger;
            _userAccountRepository = userAccountRepository;
            _photoRepository = photoRepository;
        }

        public async Task<GalleryResponse> Handle(GalleryRequest request, CancellationToken cancellationToken)
        {
            var user = await _userAccountRepository.GetUserAccountAsync(request.User);
            var photo = await _photoRepository.GetAsync(user, request.PhotoId);
            if (photo == null || photo.Filename != request.PhotoFilename)
            {
                _logger.LogInformation($"Could not find photo [{request.PhotoId}] for user [{user.UserAccountId}] with filename [{request.PhotoFilename}]");
                return new GalleryResponse(null, null, null, 0, 0);
            }

            // TODO: loading all photos just to get previous & next is bad
            var allPhotos = await _photoRepository.GetAllAsync(user);
            var photoIndex = allPhotos.FindIndex(p => p.PhotoId == photo.PhotoId);
            var previous = photoIndex > 0 ? allPhotos[photoIndex - 1] : null;
            var next = photoIndex + 1 < allPhotos.Count ? allPhotos[photoIndex + 1] : null;

            return new GalleryResponse(ToModel(photo), ToModel(previous), ToModel(next), photoIndex + 1, allPhotos.Count);
        }

        private PhotoModel? ToModel(Photo? photo) => photo == null ? null : new PhotoModel(photo.PhotoId, photo.Filename ?? "", new Size(photo.Width, photo.Height));
    }
}