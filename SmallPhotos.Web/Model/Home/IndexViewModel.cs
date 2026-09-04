using SmallPhotos.Model;

namespace SmallPhotos.Web.Model.Home;

public class IndexViewModel : BaseViewModel
{
    public IndexViewModel(HttpContext context, ThumbnailSize thumbnailSize, IEnumerable<PhotoModel> photos, Pagination pagination,
        bool showDetails, SelectedView selectedView, DateTime? minFilterDate, DateTime? maxFilterDate, string? withTag = null,
        DateTime? filterFromDate = null, DateTime? filterToDate = null)
        : base(context, selectedView)
    {
        ThumbnailSize = thumbnailSize;
        Photos = photos;
        Pagination = pagination;
        ShowDetails = showDetails;
        WithTag = withTag;

        if (Photos.Any())
        {
            var firstPhotoByDate = Photos.OrderBy(p => p.DateTimeTaken).First();
            var lastPhotoByDate = Photos.OrderByDescending(p => p.DateTimeTaken).First();
            if (firstPhotoByDate.DateTaken == lastPhotoByDate.DateTaken)
                ImageDateRange = firstPhotoByDate.DateTaken;
            else if (firstPhotoByDate.DateTimeTaken.Date == lastPhotoByDate.DateTimeTaken.Date)
                ImageDateRange = $"{lastPhotoByDate.DateTaken} - {firstPhotoByDate.DateTaken}";
            else
                ImageDateRange = $"{lastPhotoByDate.DateTimeTaken:dd MMM yyyy} - {firstPhotoByDate.DateTimeTaken:dd MMM yyyy}";

            FilterDateEnabled = (minFilterDate ?? firstPhotoByDate.DateTimeTaken).Date != (maxFilterDate ?? lastPhotoByDate.DateTimeTaken).Date;
            FilterDateUrlPart = filterFromDate != null && filterToDate != null ? $"&fromDate={filterFromDate:yyyy-MM-dd}&toDate={filterToDate:yyyy-MM-dd}" : "";
            FilterDateFrom = (filterFromDate ?? minFilterDate ?? firstPhotoByDate.DateTimeTaken).ToString("dd MMM yyyy");
            FilterDateTo = (filterToDate ?? maxFilterDate ?? lastPhotoByDate.DateTimeTaken).ToString("dd MMM yyyy");
            FilterDateMin = (minFilterDate ?? firstPhotoByDate.DateTimeTaken).ToString("yyyy-MM-dd");
            FilterDateMax = (maxFilterDate ?? lastPhotoByDate.DateTimeTaken).ToString("yyyy-MM-dd");
        }
        else
        {
            ImageDateRange = "";
            FilterDateEnabled = false;
            FilterDateUrlPart = "";
            FilterDateFrom = "";
            FilterDateTo = "";
            FilterDateMin = "";
            FilterDateMax = "";
        }
    }

    public ThumbnailSize ThumbnailSize { get; }
    public bool ShowDetails { get; }
    public IEnumerable<PhotoModel> Photos { get; }
    public Pagination Pagination { get; }
    public string ImageDateRange { get; }
    public string? WithTag { get; }
    public bool FilterDateEnabled { get; }
    public string FilterDateUrlPart { get; }
    public string FilterDateFrom { get; }
    public string FilterDateTo { get; }
    public string FilterDateMin { get; }
    public string FilterDateMax { get; }
}