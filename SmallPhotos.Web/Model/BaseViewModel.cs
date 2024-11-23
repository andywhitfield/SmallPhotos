using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model;

public abstract class BaseViewModel(HttpContext context, SelectedView selectedView)
{
    public bool IsLoggedIn { get; } = context.User?.Identity?.IsAuthenticated ?? false;
    public bool ViewAll => selectedView == SelectedView.All;
    public bool ViewStarred => selectedView == SelectedView.Starred;
    public bool ViewTagged => selectedView == SelectedView.Tagged;
}