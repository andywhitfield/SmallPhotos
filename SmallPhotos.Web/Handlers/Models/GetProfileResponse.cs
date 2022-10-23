using System.Collections.Generic;
using SmallPhotos.Model;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers.Models
{
    public class GetProfileResponse
    {
        public GetProfileResponse(IEnumerable<AlbumSourceFolderModel> folders, ThumbnailSize thumbnailSize, int galleryImagePageSize)
        {
            Folders = folders;
            ThumbnailSize = thumbnailSize;
            GalleryImagePageSize = galleryImagePageSize;
        }

        public IEnumerable<AlbumSourceFolderModel> Folders { get; }
        public ThumbnailSize ThumbnailSize { get; }
        public int GalleryImagePageSize { get; }
    }
}