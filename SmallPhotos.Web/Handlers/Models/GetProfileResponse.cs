using System.Collections.Generic;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers.Models
{
    public class GetProfileResponse
    {
        public GetProfileResponse(IEnumerable<AlbumSourceFolderModel> folders) => Folders = folders;

        public IEnumerable<AlbumSourceFolderModel> Folders { get; }
    }
}