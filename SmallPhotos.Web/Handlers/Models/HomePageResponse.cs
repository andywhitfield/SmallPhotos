using SmallPhotos.Model;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers.Models;

public class HomePageResponse(bool isUserValid, ThumbnailSize thumbnailSize,
    IEnumerable<PhotoModel> photos, Pagination pagination, bool showDetails)
{
    public bool IsUserValid { get; } = isUserValid;
    public ThumbnailSize ThumbnailSize { get; } = thumbnailSize;
    public IEnumerable<PhotoModel> Photos { get; } = photos;
    public Pagination Pagination { get; } = pagination;
    public bool ShowDetails { get; } = showDetails;
}