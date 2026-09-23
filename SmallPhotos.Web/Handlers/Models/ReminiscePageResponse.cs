using SmallPhotos.Model;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers.Models;

public class ReminiscePageResponse(bool isUserValid, ThumbnailSize thumbnailSize,
    IEnumerable<PhotoModel> photos, bool showDetails)
{
    public bool IsUserValid { get; } = isUserValid;
    public ThumbnailSize ThumbnailSize { get; } = thumbnailSize;
    public IEnumerable<PhotoModel> Photos { get; } = photos;
    public bool ShowDetails { get; } = showDetails;
}