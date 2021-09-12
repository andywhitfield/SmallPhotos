using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models
{
    public class GetPhotoRequest : IRequest<GetPhotoResponse>
    {
        public ClaimsPrincipal User { get; }
        public long PhotoId { get; }
        public string Name {get;}

        public GetPhotoRequest(ClaimsPrincipal user, long photoId, string name) => User = user;
    }
}