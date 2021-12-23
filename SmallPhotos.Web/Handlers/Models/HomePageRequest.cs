using System.Security.Claims;
using MediatR;
using SmallPhotos.Model;

namespace SmallPhotos.Web.Handlers.Models
{
    public class HomePageRequest : IRequest<HomePageResponse>
    {
        public ClaimsPrincipal User { get; }
        public ThumbnailSize ThumbnailSize { get; }

        public HomePageRequest(ClaimsPrincipal user, ThumbnailSize thumbnailSize)
        {
            User = user;
            ThumbnailSize = thumbnailSize;
        }
    }
}