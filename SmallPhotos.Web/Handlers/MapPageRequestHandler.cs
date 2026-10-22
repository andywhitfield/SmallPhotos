using MediatR;
using Microsoft.EntityFrameworkCore;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers;

public class MapPageRequestHandler(
    ILogger<MapPageRequestHandler> logger,
    IUserAccountRepository userAccountRepository,
    IPhotoRepository photoRepository)
    : IRequestHandler<MapPageRequest, MapPageResponse>
{
    public async Task<MapPageResponse> Handle(MapPageRequest request, CancellationToken cancellationToken)
    {
        var user = await userAccountRepository.GetUserAccountOrNullAsync(request.User);
        if (user == null)
        {
            logger.LogInformation("No active user account, user [{RequestUserIdentityName}] is not valid", request.User.Identity?.Name);
            return new(false, []);
        }

        var photos = await photoRepository.GetAsync(user,
            request.PhotoIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(id => long.TryParse(id, out var l) ? l : default(long?))
                .Where(id => id != null)
                .Select(id => id!.Value))
            .Where(p => p.Latitude != null && p.Longitude != null).ToListAsync();
        return new(true, photos.Select(p => new PhotoModel(p.PhotoId, "", default, p.Filename ?? "",
            p.RelativePath ?? "", default, p.DateTaken ?? p.FileCreationDateTime, p.FileCreationDateTime,
            false, Array.Empty<string>(), p.Latitude, p.Longitude, p.GeoLocality, p.GeoCity,
            p.GeoPrincipalSubdivision, p.GeoCountryName)));
    }
}
