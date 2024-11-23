using System.Security.Claims;
using MediatR;
using SmallPhotos.Model;

namespace SmallPhotos.Web.Handlers.Models;

public class UpdateUserThumbnailSizeRequest(ClaimsPrincipal user, ThumbnailSize newThumbnailSize)
    : IRequest<bool>
{
    public ClaimsPrincipal User { get; } = user;
    public ThumbnailSize NewThumbnailSize { get; } = newThumbnailSize;
}