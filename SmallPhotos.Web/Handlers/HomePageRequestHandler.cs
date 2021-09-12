using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers
{
    public class HomePageRequestHandler : IRequestHandler<HomePageRequest, HomePageResponse>
    {
        private readonly ILogger<HomePageRequestHandler> _logger;
        private readonly IUserAccountRepository _userAccountRepository;

        public HomePageRequestHandler(ILogger<HomePageRequestHandler> logger, IUserAccountRepository userAccountRepository)
        {
            _logger = logger;
            _userAccountRepository = userAccountRepository;
        }

        public async Task<HomePageResponse> Handle(HomePageRequest request, CancellationToken cancellationToken)
        {
            /*
            var dir = new DirectoryInfo("/Users/andywhitfield/Dropbox/Camera uploads");
            if (!dir.Exists)
            {
                _logger.LogWarning("Photo directory does not exist, returning empty photo list");
                return Task.FromResult(new HomePageResponse(Enumerable.Empty<PhotoModel>()));
            }

            var photos = dir
                .EnumerateFiles()
                .Where(f => _validExtensions.Contains(f.Extension.ToLowerInvariant()))
                .OrderByDescending(f => f.CreationTimeUtc)
                .Select(f => new PhotoModel(f.Name))
                .Take(100);
            _logger.LogTrace($"Found {photos.Count()} photos");

            return Task.FromResult(new HomePageResponse(photos));
            */
            //return Task.FromResult(new HomePageResponse(Enumerable.Empty<PhotoModel>()));

            var user = await _userAccountRepository.GetUserAccountAsync(request.User);
            return new HomePageResponse(new[] { new PhotoModel(1, "test1.jpg", new System.Drawing.Size(500, 500)) });
        }
    }
}