using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models;

public class HomePageRequest : IRequest<HomePageResponse>
{
    public ClaimsPrincipal User { get; }
    public int PageNumber { get; }
    public int? PhotoId { get; }
    public bool OnlyStarred { get; }

    public HomePageRequest(ClaimsPrincipal user, int pageNumber, int? photoId, bool onlyStarred = false)
    {
        User = user;
        PageNumber = pageNumber;
        PhotoId = photoId;
        OnlyStarred = onlyStarred;
    }
}