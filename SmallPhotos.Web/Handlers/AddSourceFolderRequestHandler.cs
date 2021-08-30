using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers
{
    public class AddSourceFolderRequestHandler : IRequestHandler<AddSourceFolderRequest, bool>
    {
        private readonly ILogger<AddSourceFolderRequestHandler> _logger;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IAlbumRepository _albumRepository;

        public AddSourceFolderRequestHandler(
            ILogger<AddSourceFolderRequestHandler> logger,
            IUserAccountRepository userAccountRepository,
            IAlbumRepository albumRepository)
        {
            _logger = logger;
            _userAccountRepository = userAccountRepository;
            _albumRepository = albumRepository;
        }

        public async Task<bool> Handle(AddSourceFolderRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Folder))
                return false;
            
            if (!Path.IsPathRooted(request.Folder))
            {
                _logger.LogWarning($"Cannot add a folder which isn't a fully-qualified path [{request.Folder}]");
                return false;
            }

            var user = await _userAccountRepository.GetUserAccountAsync(request.User);
            await _albumRepository.AddAlbumSourceAsync(user, request.Folder);
            return true;
        }
    }
}