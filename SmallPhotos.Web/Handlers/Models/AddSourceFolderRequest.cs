using System.Security.Claims;
using MediatR;

namespace SmallPhotos.Web.Handlers.Models
{
    public class AddSourceFolderRequest : IRequest<bool>
    {
        public AddSourceFolderRequest(ClaimsPrincipal user, string folder)
        {
            User = user;
            Folder = folder;
        }

        public ClaimsPrincipal User { get; }

        public string Folder { get; }
    }
}