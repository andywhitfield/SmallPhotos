using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models;

public class DeleteSourceFolderRequest : IRequest<bool>
{
    public DeleteSourceFolderRequest(ClaimsPrincipal user, long albumSourceId)
    {
        User = user;
        AlbumSourceId = albumSourceId;
    }

    public ClaimsPrincipal User { get; }

    public long AlbumSourceId { get; }
}