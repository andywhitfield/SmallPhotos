using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models;

public class GetProfileRequest(ClaimsPrincipal user)
    : IRequest<GetProfileResponse>
{
    public ClaimsPrincipal User { get; } = user;
}