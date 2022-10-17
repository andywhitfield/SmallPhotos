using System.Security.Claims;
using MediatR;
using SmallPhotos.Model;

namespace SmallPhotos.Web.Handlers.Models
{
    public class UpdateUserThumbnailSizeRequest : IRequest<bool>
    {
        public ClaimsPrincipal User { get; }
        public ThumbnailSize NewThumbnailSize { get; }

        public UpdateUserThumbnailSizeRequest(ClaimsPrincipal user, ThumbnailSize newThumbnailSize)
        {
            User = user;
            NewThumbnailSize = newThumbnailSize;
        }
    }
}