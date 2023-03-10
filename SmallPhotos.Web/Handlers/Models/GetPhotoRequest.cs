using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models;

public class GetPhotoRequest : IRequest<GetPhotoResponse>
{
    public ClaimsPrincipal User { get; }
    public long PhotoId { get; }
    public string Name { get; }
    public string? ThumbnailSize { get; }
    public bool Original { get; }

    public GetPhotoRequest(ClaimsPrincipal user, long photoId, string name, string? thumbnailSize, bool original)
    {
        User = user;
        PhotoId = photoId;
        Name = name;
        ThumbnailSize = thumbnailSize;
        Original = original;
    }
}