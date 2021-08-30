using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model.Profile
{
    public class IndexViewModel : BaseViewModel
    {
        public IndexViewModel(HttpContext context, IEnumerable<AlbumSourceFolderModel> folders) : base(context)
            => Folders = folders;

        public IEnumerable<AlbumSourceFolderModel> Folders { get; }
    }
}