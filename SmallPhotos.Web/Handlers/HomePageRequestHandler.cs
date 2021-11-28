using System.Linq;
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
    public class HomePageRequestHandler : IRequestHandler<HomePageRequest, HomePageResponse>
    {
        private readonly ILogger<HomePageRequestHandler> _logger;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IPhotoRepository _photoRepository;

        public HomePageRequestHandler(ILogger<HomePageRequestHandler> logger, IUserAccountRepository userAccountRepository,
            IPhotoRepository photoRepository)
        {
            _logger = logger;
            _userAccountRepository = userAccountRepository;
            _photoRepository = photoRepository;
        }

        public async Task<HomePageResponse> Handle(HomePageRequest request, CancellationToken cancellationToken)
        {
            var user = await _userAccountRepository.GetUserAccountOrNullAsync(request.User);
            if (user == null)
                return new HomePageResponse(false, Enumerable.Empty<PhotoModel>());

            var photos = await _photoRepository.GetAllAsync(user);
            return new HomePageResponse(true, photos.Select(p => new PhotoModel(p.PhotoId, p.Filename, request.ThumbnailSize.ToSize())));
        }
    }
}