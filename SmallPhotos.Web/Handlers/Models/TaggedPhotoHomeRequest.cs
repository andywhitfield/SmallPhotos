using System.Collections.Generic;
using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models;

public class TaggedPhotoHomeRequest(ClaimsPrincipal user)
    : IRequest<IEnumerable<(string Tag, int PhotoCount)>>
{
    public ClaimsPrincipal User { get; } = user;
}