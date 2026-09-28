using MediatR;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers;

public class GetProfileRequestHandler(
    IUserAccountRepository userAccountRepository,
    IAlbumRepository albumRepository,
    IUserFeedRepository userFeedRepository)
    : IRequestHandler<GetProfileRequest, GetProfileResponse>
{
    public async Task<GetProfileResponse> Handle(GetProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await userAccountRepository.GetUserAccountAsync(request.User);
        var allAlbumSources = await albumRepository.GetAllAsync(user);
        var userFeed = await userFeedRepository.GetAsync(user);
        return new(allAlbumSources.Select(a => new AlbumSourceFolderModel(a.AlbumSourceId, (a.IsDropboxSource ? "[Dropbox] " : "") + a.Folder ?? "", a.RecurseSubFolders ?? false)),
            user.ThumbnailSize, user.GalleryImagePageSize ?? Pagination.DefaultPageSize, userFeed);
    }
}