namespace SmallPhotos.Web.Model.Map;

public class IndexViewModel(HttpContext context, IEnumerable<PhotoModel> photos, SelectedView selectedView)
    : BaseViewModel(context, selectedView)
{
    public IEnumerable<PhotoModel> Photos { get; } = photos;
}
