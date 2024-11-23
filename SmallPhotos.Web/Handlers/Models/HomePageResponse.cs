using System.Collections.Generic;
using SmallPhotos.Model;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers.Models;

public class HomePageResponse(bool isUserValid, ThumbnailSize thumbnailSize,
    IEnumerable<PhotoModel> photos, Pagination pagination)
{
    public bool IsUserValid { get; } = isUserValid;
    public ThumbnailSize ThumbnailSize { get; } = thumbnailSize;
    public IEnumerable<PhotoModel> Photos { get; } = photos;
    public Pagination Pagination { get; } = pagination;
}