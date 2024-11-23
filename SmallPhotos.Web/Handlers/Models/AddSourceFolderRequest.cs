using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models;

public class AddSourceFolderRequest(ClaimsPrincipal user, string folder, bool recursive,
    string? dropboxAccessToken = null, string? dropboxRefreshToken = null)
    : IRequest<bool>
{
    public ClaimsPrincipal User { get; } = user;
    public string Folder { get; } = folder;
    public bool Recursive { get; } = recursive;
    public string? DropboxAccessToken { get; } = dropboxAccessToken;
    public string? DropboxRefreshToken { get; } = dropboxRefreshToken;
}