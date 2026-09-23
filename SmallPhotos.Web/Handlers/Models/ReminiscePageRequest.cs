using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models;

public class ReminiscePageRequest(ClaimsPrincipal user) : IRequest<ReminiscePageResponse>
{
    public ClaimsPrincipal User { get; } = user;
}