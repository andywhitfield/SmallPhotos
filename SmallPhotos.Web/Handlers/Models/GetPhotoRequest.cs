using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models;

public class GetPhotoRequest(ClaimsPrincipal user, long photoId, string name,
    string? thumbnailSize, bool original)
    : IRequest<GetPhotoResponse>
{
    public ClaimsPrincipal User { get; } = user;
    public long PhotoId { get; } = photoId;
    public string Name { get; } = name;
    public string? ThumbnailSize { get; } = thumbnailSize;
    public bool Original { get; } = original;
}