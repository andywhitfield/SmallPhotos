using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models
{
    public class HomePageRequest : IRequest<HomePageResponse>
    {
        public ClaimsPrincipal User { get; }

        public HomePageRequest(ClaimsPrincipal user) => User = user;
    }
}