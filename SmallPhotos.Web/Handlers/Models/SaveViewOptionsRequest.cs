using System.Security.Claims;
using MediatR;
using SmallPhotos.Model;

namespace SmallPhotos.Web.Handlers.Models
{
    public class SaveViewOptionsRequest : IRequest<bool>
    {
        public ClaimsPrincipal User { get; }
        public ThumbnailSize NewThumbnailSize { get; }
        public int NewPageSize { get; }

        public SaveViewOptionsRequest(ClaimsPrincipal user, ThumbnailSize newThumbnailSize, int newPageSize)
        {
            User = user;
            NewThumbnailSize = newThumbnailSize;
            NewPageSize = newPageSize;
        }
    }
}