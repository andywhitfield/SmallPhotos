using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models;

public class GalleryRequest : IRequest<GalleryResponse>
{
    public ClaimsPrincipal User { get; }
    public long PhotoId { get; }
    public string PhotoFilename { get; }
    public bool OnlyStarred { get; }
    public string? WithTag { get; }

    public GalleryRequest(ClaimsPrincipal user, long photoId, string photoFilename, bool onlyStarred = false, string? withTag = null)
    {
        User = user;
        PhotoId = photoId;
        PhotoFilename = photoFilename;
        OnlyStarred = onlyStarred;
        WithTag = withTag;
    }
}