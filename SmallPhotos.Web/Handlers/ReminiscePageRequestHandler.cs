using MediatR;
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers;

public class ReminiscePageRequestHandler(
    ILogger<HomePageRequestHandler> logger,
    TimeProvider timeProvider,
    IUserAccountRepository userAccountRepository,
    IPhotoRepository photoRepository)
    : IRequestHandler<ReminiscePageRequest, ReminiscePageResponse>
{
    private const int _dayRange = 16;
    private const int _reminisceCount = 6;
    private readonly Random _random = new();

    public async Task<ReminiscePageResponse> Handle(ReminiscePageRequest request, CancellationToken cancellationToken)
    {
        var user = await userAccountRepository.GetUserAccountOrNullAsync(request.User);
        if (user == null)
        {
            logger.LogInformation("No active user account, user [{RequestUserIdentityName}] is not valid", request.User.Identity?.Name);
            return new(false, ThumbnailSize.Small, [], false);
        }

        var now = timeProvider.GetUtcNow();
        var photos = await photoRepository.GetPreviousYearPhotosAsync(user, now, _dayRange).ToListAsync(cancellationToken: cancellationToken);

        if (photos.Count != 0)
        {
            var dayDiffs = photos.Select(p => (Photo: p, DayDiff: Math.Abs((now.Ticks - (p.DateTaken ?? p.FileCreationDateTime).Ticks) / TimeSpan.TicksPerDay % 365))).ToList();
            var upperBound = dayDiffs.Sum(d => d.DayDiff > _dayRange ? 365 - d.DayDiff : d.DayDiff);
            photos = [.. dayDiffs.OrderBy(d =>
            {
                var r = _random.NextInt64(upperBound);
                if (r > d.DayDiff)
                    return _random.NextInt64(d.DayDiff);
                return r;
            })
            .Select(d => d.Photo)
            .Take(_reminisceCount)];
        }

        return new(
            true,
            ThumbnailSize.Large,
            photos.Select(p => new PhotoModel(p.PhotoId, p.AlbumSource?.Folder ?? "", p.AlbumSource?.IsDropboxSource ?? false, p.Filename ?? "", p.RelativePath ?? "", ThumbnailSize.Large.ToSize(), p.DateTaken ?? p.FileCreationDateTime, p.FileCreationDateTime, false, [])),
            true);
    }
}