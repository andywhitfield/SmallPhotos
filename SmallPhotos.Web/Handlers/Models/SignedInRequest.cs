using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models
{
    public class SignedInRequest : IRequest<string>
    {
        public ClaimsPrincipal User { get; }
        public string ReturnUrl { get; }
        public SignedInRequest(ClaimsPrincipal user, string returnUrl)
        {
            User = user;
            ReturnUrl = returnUrl;
        }
    }
}