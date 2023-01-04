using System.Collections.Generic;
using System.Drawing;
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
        var allPhotos = await (request.OnlyStarred
            ? _photoRepository.GetAllStarredAsync(user)
            : !string.IsNullOrWhiteSpace(request.WithTag) ? _photoRepository.GetAllWithTagAsync(user, request.WithTag)
            : _photoRepository.GetAllAsync(user));
        var photoIndex = allPhotos.FindIndex(p => p.PhotoId == photo.PhotoId);
        var previous = photoIndex > 0 ? allPhotos[photoIndex - 1] : null;
        var next = photoIndex + 1 < allPhotos.Count ? allPhotos[photoIndex + 1] : null;

        var starredPhotos = (await _photoRepository.GetStarredAsync(user, new[] { photo, previous, next }.Where(p => p != null).Select(p => p!.PhotoId).ToHashSet())).Select(p => p.PhotoId).ToHashSet();

        return new GalleryResponse(await ToModelAsync(user, photo, starredPhotos), await ToModelAsync(null, previous, starredPhotos), await ToModelAsync(null, next, starredPhotos), photoIndex + 1, allPhotos.Count);
    }

    private async Task<PhotoModel?> ToModelAsync(UserAccount? user, Photo? photo, IEnumerable<long> starredPhotos) => photo == null ? null : new PhotoModel(photo.PhotoId, photo.AlbumSource?.Folder ?? "", photo.Filename ?? "", photo.RelativePath ?? "", new Size(photo.Width, photo.Height), photo.DateTaken ?? photo.FileCreationDateTime, photo.FileCreationDateTime, starredPhotos.Contains(photo.PhotoId), user == null ? Enumerable.Empty<string>() : (await _photoRepository.GetTagsAsync(user, photo)).Select(t => t.Tag));
}