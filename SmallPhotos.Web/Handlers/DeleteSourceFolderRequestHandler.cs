using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers;

public class DeleteSourceFolderRequestHandler(
    IUserAccountRepository userAccountRepository,
    IAlbumRepository albumRepository)
    : IRequestHandler<DeleteSourceFolderRequest, bool>
{
    public async Task<bool> Handle(DeleteSourceFolderRequest request, CancellationToken cancellationToken)
    {
        var user = await userAccountRepository.GetUserAccountAsync(request.User);
        var albumSource = await albumRepository.GetAsync(user, request.AlbumSourceId);
        if (albumSource == null)
            return false;

        await albumRepository.DeleteAlbumSourceAsync(albumSource);
        return true;
    }
}