using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models
{
    public class GalleryRequest : IRequest<GalleryResponse>
    {
        public ClaimsPrincipal User { get; }
        public long PhotoId { get; }
        public string PhotoFilename { get; }

        public GalleryRequest(ClaimsPrincipal user, long photoId, string photoFilename)
        {
            User = user;
            PhotoId = photoId;
            PhotoFilename = photoFilename;
        }
    }
}