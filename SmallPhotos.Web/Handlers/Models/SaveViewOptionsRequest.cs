using System.Security.Claims;
using MediatR;
using SmallPhotos.Model;

namespace SmallPhotos.Web.Handlers.Models;

public class SaveViewOptionsRequest(ClaimsPrincipal user, ThumbnailSize newThumbnailSize,
    int newPageSize)
    : IRequest<bool>
{
    public ClaimsPrincipal User { get; } = user;
    public ThumbnailSize NewThumbnailSize { get; } = newThumbnailSize;
    public int NewPageSize { get; } = newPageSize;
}