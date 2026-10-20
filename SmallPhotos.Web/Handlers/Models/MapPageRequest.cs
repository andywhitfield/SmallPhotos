using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models;

public class MapPageRequest(ClaimsPrincipal user, string photoIds)
    : IRequest<MapPageResponse>
{
    public ClaimsPrincipal User { get; } = user;
    public string PhotoIds { get; } = photoIds;
}