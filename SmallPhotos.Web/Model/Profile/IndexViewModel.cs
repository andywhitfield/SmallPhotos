using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using SmallPhotos.Model;

namespace SmallPhotos.Web.Model.Profile;

public class IndexViewModel(HttpContext context,
    IEnumerable<AlbumSourceFolderModel> folders,
    ThumbnailSize thumbnailSize,
    int galleryImagePageSize,
    string? dropboxAccessToken = null,
    string? dropboxRefreshToken = null)
    : BaseViewModel(context, SelectedView.None)
{
    public IEnumerable<AlbumSourceFolderModel> Folders { get; } = folders;
    public ThumbnailSize ThumbnailSize { get; } = thumbnailSize;
    public int GalleryImagePageSize { get; } = galleryImagePageSize;
    public string? DropboxAccessToken { get; } = dropboxAccessToken;
    public string? DropboxRefreshToken { get; } = dropboxRefreshToken;
}