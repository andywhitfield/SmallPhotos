using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers
{
    public class DeleteSourceFolderRequestHandler : IRequestHandler<DeleteSourceFolderRequest, bool>
    {
        private readonly ILogger<DeleteSourceFolderRequestHandler> _logger;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IAlbumRepository _albumRepository;

        public DeleteSourceFolderRequestHandler(
            ILogger<DeleteSourceFolderRequestHandler> logger,
            IUserAccountRepository userAccountRepository,
            IAlbumRepository albumRepository)
        {
            _logger = logger;
            _userAccountRepository = userAccountRepository;
            _albumRepository = albumRepository;
        }

        public async Task<bool> Handle(DeleteSourceFolderRequest request, CancellationToken cancellationToken)
        {
            var user = await _userAccountRepository.GetUserAccountAsync(request.User);
            var albumSource = await _albumRepository.GetAlbumSourceAsync(user, request.AlbumSourceId);
            if (albumSource == null)
                return false;

            await _albumRepository.DeleteAlbumSourceAsync(albumSource);
            return true;
        }
    }
}