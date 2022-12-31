using System.Collections.Generic;
using SmallPhotos.Model;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers.Models;

public class HomePageResponse
{
    public HomePageResponse(bool isUserValid, ThumbnailSize thumbnailSize, IEnumerable<PhotoModel> photos, Pagination pagination)
    {
        IsUserValid = isUserValid;
        ThumbnailSize = thumbnailSize;
        Photos = photos;
        Pagination = pagination;
    }

    public bool IsUserValid { get; }
    public ThumbnailSize ThumbnailSize { get; }
    public IEnumerable<PhotoModel> Photos { get; }
    public Pagination Pagination { get; }
}