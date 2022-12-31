using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models;

public class AddSourceFolderRequest : IRequest<bool>
{
    public AddSourceFolderRequest(ClaimsPrincipal user, string folder, bool recursive)
    {
        User = user;
        Folder = folder;
        Recursive = recursive;
    }

    public ClaimsPrincipal User { get; }

    public string Folder { get; }

    public bool Recursive { get; }
}