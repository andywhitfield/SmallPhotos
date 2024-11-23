using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models;

public class HomePageRequest(ClaimsPrincipal user, int pageNumber, int? photoId,
    bool onlyStarred = false, string? withTag = null)
    : IRequest<HomePageResponse>
{
    public ClaimsPrincipal User { get; } = user;
    public int PageNumber { get; } = pageNumber;
    public int? PhotoId { get; } = photoId;
    public bool OnlyStarred { get; } = onlyStarred;
    public string? WithTag { get; } = withTag;
}