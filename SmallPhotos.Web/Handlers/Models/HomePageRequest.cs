using System.Security.Claims;
using MediatR;
using SmallPhotos.Model;

namespace SmallPhotos.Web.Handlers.Models
{
    public class HomePageRequest : IRequest<HomePageResponse>
    {
        public ClaimsPrincipal User { get; }
        public ThumbnailSize ThumbnailSize { get; }
        public int PageNumber { get; }
        public int? PhotoId { get; }

        public HomePageRequest(ClaimsPrincipal user, ThumbnailSize thumbnailSize, int pageNumber, int? photoId)
        {
            User = user;
            ThumbnailSize = thumbnailSize;
            PageNumber = pageNumber;
            PhotoId = photoId;
        }
    }
}