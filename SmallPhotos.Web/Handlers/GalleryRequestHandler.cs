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

public class GalleryRequestHandler(ILogger<GalleryRequestHandler> logger, IUserAccountRepository userAccountRepository,
    IPhotoRepository photoRepository)
    : IRequestHandler<GalleryRequest, GalleryResponse>
{
    public async Task<GalleryResponse> Handle(GalleryRequest request, CancellationToken cancellationToken)
    {
        var user = await userAccountRepository.GetUserAccountAsync(request.User);
        var photo = await photoRepository.GetAsync(user, request.PhotoId);
        if (photo == null || photo.Filename != request.PhotoFilename)
        {
            logger.LogInformation("Could not find photo [{RequestPhotoId}] for user [{UserAccountId}] with filename [{RequestPhotoFilename}]", request.PhotoId, user.UserAccountId, request.PhotoFilename);
            return new GalleryResponse(null, null, null, 0, 0);
        }

        // TODO: loading all photos just to get previous & next is bad
        var allPhotos = await (request.OnlyStarred
            ? photoRepository.GetAllStarredAsync(user)
            : !string.IsNullOrWhiteSpace(request.WithTag) ? photoRepository.GetAllWithTagAsync(user, request.WithTag)
            : photoRepository.GetAllAsync(user));
        var photoIndex = allPhotos.FindIndex(p => p.PhotoId == photo.PhotoId);
        var previous = photoIndex > 0 ? allPhotos[photoIndex - 1] : null;
        var next = photoIndex + 1 < allPhotos.Count ? allPhotos[photoIndex + 1] : null;

        var starredPhotos = (await photoRepository.GetStarredAsync(user, new[] { photo, previous, next }.Where(p => p != null).Select(p => p!.PhotoId).ToHashSet())).Select(p => p.PhotoId).ToHashSet();

        return new GalleryResponse(await ToModelAsync(user, photo, starredPhotos), await ToModelAsync(null, previous, starredPhotos), await ToModelAsync(null, next, starredPhotos), photoIndex + 1, allPhotos.Count);
    }

    private async Task<PhotoModel?> ToModelAsync(UserAccount? user, Photo? photo, IEnumerable<long> starredPhotos) => photo == null ? null : new PhotoModel(photo.PhotoId, photo.AlbumSource?.Folder ?? "", photo.AlbumSource?.IsDropboxSource ?? false, photo.Filename ?? "", photo.RelativePath ?? "", new Size(photo.Width, photo.Height), photo.DateTaken ?? photo.FileCreationDateTime, photo.FileCreationDateTime, starredPhotos.Contains(photo.PhotoId), user == null ? Enumerable.Empty<string>() : (await photoRepository.GetTagsAsync(user, photo)).Select(t => t.Tag));
}