using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models;

public class GalleryRequest(ClaimsPrincipal user, long photoId, string photoFilename,
    bool onlyStarred = false, string? withTag = null)
    : IRequest<GalleryResponse>
{
    public ClaimsPrincipal User { get; } = user;
    public long PhotoId { get; } = photoId;
    public string PhotoFilename { get; } = photoFilename;
    public bool OnlyStarred { get; } = onlyStarred;
    public string? WithTag { get; } = withTag;
}