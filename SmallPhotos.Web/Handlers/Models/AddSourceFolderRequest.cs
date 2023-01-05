using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models;

public class AddSourceFolderRequest : IRequest<bool>
{
    public AddSourceFolderRequest(ClaimsPrincipal user, string folder, bool recursive, string? dropboxAccessToken = null, string? dropboxRefreshToken = null)
    {
        User = user;
        Folder = folder;
        Recursive = recursive;
        DropboxAccessToken = dropboxAccessToken;
        DropboxRefreshToken = dropboxRefreshToken;
    }

    public ClaimsPrincipal User { get; }
    public string Folder { get; }
    public bool Recursive { get; }
    public string? DropboxAccessToken { get; }
    public string? DropboxRefreshToken { get; }
}