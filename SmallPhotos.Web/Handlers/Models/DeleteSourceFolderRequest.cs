using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models;

public class DeleteSourceFolderRequest(ClaimsPrincipal user, long albumSourceId)
    : IRequest<bool>
{
    public ClaimsPrincipal User { get; } = user;

    public long AlbumSourceId { get; } = albumSourceId;
}