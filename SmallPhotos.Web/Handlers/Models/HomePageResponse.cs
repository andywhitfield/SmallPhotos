using SmallPhotos.Model;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers.Models;

public class HomePageResponse(bool isUserValid, ThumbnailSize thumbnailSize,
    IEnumerable<PhotoModel> photos, Pagination pagination, bool showDetails,
    DateTime? minFilterDate, DateTime? maxFilterDate, DateTime? filterFromDate,
    DateTime? filterToDate)
{
    public bool IsUserValid { get; } = isUserValid;
    public ThumbnailSize ThumbnailSize { get; } = thumbnailSize;
    public IEnumerable<PhotoModel> Photos { get; } = photos;
    public Pagination Pagination { get; } = pagination;
    public bool ShowDetails { get; } = showDetails;
    public DateTime? MinFilterDate { get; } = minFilterDate;
    public DateTime? MaxFilterDate { get; } = maxFilterDate;
    public DateTime? FilterFromDate { get; } = filterFromDate;
    public DateTime? FilterToDate { get; } = filterToDate;
}