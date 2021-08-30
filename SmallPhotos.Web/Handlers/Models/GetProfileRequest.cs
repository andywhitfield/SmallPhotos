using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models
{
    public class GetProfileRequest : IRequest<GetProfileResponse>
    {
        public GetProfileRequest(ClaimsPrincipal user) => User = user;

        public ClaimsPrincipal User { get; }
    }
}