using SmallPhotos.Model;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers.Models;

public class GetProfileResponse(IEnumerable<AlbumSourceFolderModel> folders,
    ThumbnailSize thumbnailSize, int galleryImagePageSize,
    UserFeed? userFeed)
{
    public IEnumerable<AlbumSourceFolderModel> Folders { get; } = folders;
    public ThumbnailSize ThumbnailSize { get; } = thumbnailSize;
    public int GalleryImagePageSize { get; } = galleryImagePageSize;
    public UserFeed? UserFeed { get; } = userFeed;
}