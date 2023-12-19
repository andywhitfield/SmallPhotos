using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers;

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
        {
            _logger.LogInformation($"No active user account, user [{request.User.Identity?.Name}] is not valid");
            return new(false, ThumbnailSize.Small, Enumerable.Empty<PhotoModel>(), Pagination.Empty);
        }

        var photos =
            request.OnlyStarred ? _photoRepository.GetAllStarredAsync(user)
            : !string.IsNullOrWhiteSpace(request.WithTag) ? _photoRepository.GetAllWithTagAsync(user, request.WithTag)
            : _photoRepository.GetAllAsync(user);
        // TODO: should do better than loading everything, then taking the page size number of photos
        var pagedPhotos = Pagination.Paginate(await photos, request.PageNumber, user.GalleryImagePageSize, request.PhotoId == null ? null : photo => photo.PhotoId == request.PhotoId);
        var starredPhotoIds = (await _photoRepository.GetStarredAsync(user, pagedPhotos.Items.Select(p => p.PhotoId).ToHashSet())).Select(p => p.PhotoId).ToHashSet();

        return new(true, user.ThumbnailSize, pagedPhotos.Items.Select(p => new PhotoModel(p.PhotoId, p.AlbumSource?.Folder ?? "", p.AlbumSource?.IsDropboxSource ?? false, p.Filename ?? "", p.RelativePath ?? "", user.ThumbnailSize.ToSize(), p.DateTaken ?? p.FileCreationDateTime, p.FileCreationDateTime, starredPhotoIds.Contains(p.PhotoId), Enumerable.Empty<string>())), new Pagination(pagedPhotos.Page, pagedPhotos.PageCount));
    }
}