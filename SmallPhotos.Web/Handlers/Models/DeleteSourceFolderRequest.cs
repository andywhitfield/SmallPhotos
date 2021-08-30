using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models
{
    public class DeleteSourceFolderRequest : IRequest<bool>
    {
        public DeleteSourceFolderRequest(ClaimsPrincipal user, int albumSourceId)
        {
            User = user;
            AlbumSourceId = albumSourceId;
        }

        public ClaimsPrincipal User { get; }

        public int AlbumSourceId { get; }
    }
}