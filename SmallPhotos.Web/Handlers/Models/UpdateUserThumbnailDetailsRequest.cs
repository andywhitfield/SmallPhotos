using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models;

public class UpdateUserThumbnailDetailsRequest(ClaimsPrincipal user, bool showThumbnailDetails)
    : IRequest<bool>
{
    public ClaimsPrincipal User { get; } = user;
    public bool ShowThumbnailDetails { get; } = showThumbnailDetails;
}