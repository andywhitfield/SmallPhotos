using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using SmallPhotos.Model;

namespace SmallPhotos.Web.Model.Profile;

public class IndexViewModel : BaseViewModel
{
    public IndexViewModel(HttpContext context,
        IEnumerable<AlbumSourceFolderModel> folders,
        ThumbnailSize thumbnailSize,
        int galleryImagePageSize,
        string? dropboxAccessToken = null,
        string? dropboxRefreshToken = null) : base(context, SelectedView.None)
    {
        Folders = folders;
        ThumbnailSize = thumbnailSize;
        GalleryImagePageSize = galleryImagePageSize;
        DropboxAccessToken = dropboxAccessToken;
        DropboxRefreshToken = dropboxRefreshToken;
    }

    public IEnumerable<AlbumSourceFolderModel> Folders { get; }
    public ThumbnailSize ThumbnailSize { get; }
    public int GalleryImagePageSize { get; }
    public string? DropboxAccessToken { get; }
    public string? DropboxRefreshToken { get; }
}