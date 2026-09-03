using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers;

public class HomePageRequestHandler(ILogger<HomePageRequestHandler> logger, IUserAccountRepository userAccountRepository,
    IPhotoRepository photoRepository)
    : IRequestHandler<HomePageRequest, HomePageResponse>
{
    public async Task<HomePageResponse> Handle(HomePageRequest request, CancellationToken cancellationToken)
    {
        var user = await userAccountRepository.GetUserAccountOrNullAsync(request.User);
        if (user == null)
        {
            logger.LogInformation("No active user account, user [{RequestUserIdentityName}] is not valid", request.User.Identity?.Name);
            return new(false, ThumbnailSize.Small, [], Pagination.Empty, false, default, default, default, default);
        }

        var photos =
            request.OnlyStarred ? photoRepository.GetAllStarred(user)
            : !string.IsNullOrWhiteSpace(request.WithTag) ? photoRepository.GetAllWithTag(user, request.WithTag)
            : photoRepository.GetAll(user);

        var photoTakenDate = await photos.AsAsyncEnumerable().AggregateAsync(
            new { Min = default(DateTime?), Max = default(DateTime?) },
            (acc, p) => new
            {
                Min = acc.Min == null || (p.DateTaken ?? p.FileCreationDateTime) < acc.Min ? (p.DateTaken ?? p.FileCreationDateTime) : acc.Min,
                Max = acc.Max == null || (p.DateTaken ?? p.FileCreationDateTime) > acc.Max ? (p.DateTaken ?? p.FileCreationDateTime) : acc.Max
            },
            cancellationToken: cancellationToken);

        DateTime filterFromDate = default;
        DateTime filterToDate = default;
        if (request.FromDate != null && DateTime.TryParseExact(request.FromDate, "yyyy-MM-dd", null, DateTimeStyles.AssumeUniversal, out filterFromDate) &&
            request.ToDate != null && DateTime.TryParseExact(request.ToDate, "yyyy-MM-dd", null, DateTimeStyles.AssumeUniversal, out filterToDate))
        {
            photos = photos.Where(p => (p.DateTaken ?? p.FileCreationDateTime).Date >= filterFromDate.Date && (p.DateTaken ?? p.FileCreationDateTime).Date <= filterToDate.Date);
        }

        // TODO: should do better than loading everything, then taking the page size number of photos
        var pagedPhotos = Pagination.Paginate(await photos.ToListAsync(cancellationToken), request.PageNumber, user.GalleryImagePageSize, request.PhotoId == null ? null : photo => photo.PhotoId == request.PhotoId);
        var starredPhotoIds = (await photoRepository.GetStarredAsync(user, pagedPhotos.Items.Select(p => p.PhotoId).ToHashSet())).Select(p => p.PhotoId).ToHashSet();

        return new(
            true,
            user.ThumbnailSize,
            pagedPhotos.Items.Select(p => new PhotoModel(p.PhotoId, p.AlbumSource?.Folder ?? "", p.AlbumSource?.IsDropboxSource ?? false, p.Filename ?? "", p.RelativePath ?? "", user.ThumbnailSize.ToSize(), p.DateTaken ?? p.FileCreationDateTime, p.FileCreationDateTime, starredPhotoIds.Contains(p.PhotoId), [])),
            new Pagination(pagedPhotos.Page, pagedPhotos.PageCount),
            user.GalleryShowDetails ?? false,
            photoTakenDate.Min,
            photoTakenDate.Max,
            filterFromDate == default ? null : filterFromDate,
            filterToDate == default ? null : filterToDate);
    }
}