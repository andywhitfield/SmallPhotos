using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers;

public class GetProfileRequestHandler : IRequestHandler<GetProfileRequest, GetProfileResponse>
{
    private readonly ILogger<GetProfileRequestHandler> _logger;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAlbumRepository _albumRepository;

    public GetProfileRequestHandler(
        ILogger<GetProfileRequestHandler> logger,
        IUserAccountRepository userAccountRepository,
        IAlbumRepository albumRepository)
    {
        _logger = logger;
        _userAccountRepository = userAccountRepository;
        _albumRepository = albumRepository;
    }

    public async Task<GetProfileResponse> Handle(GetProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await _userAccountRepository.GetUserAccountAsync(request.User);
        var allAlbumSources = await _albumRepository.GetAllAsync(user);
        return new GetProfileResponse(allAlbumSources.Select(a => new AlbumSourceFolderModel(a.AlbumSourceId, a.Folder ?? "", a.RecurseSubFolders ?? false)),
            user.ThumbnailSize, user.GalleryImagePageSize ?? Pagination.DefaultPageSize);
    }
}