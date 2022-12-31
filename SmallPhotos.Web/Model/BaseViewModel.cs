using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model;

public abstract class BaseViewModel
{
    private readonly SelectedView _selectedView;

    protected BaseViewModel(HttpContext context, SelectedView selectedView)
    {
        IsLoggedIn = context.User?.Identity?.IsAuthenticated ?? false;
        _selectedView = selectedView;
    }

    public bool IsLoggedIn { get; }
    public bool ViewAll => _selectedView == SelectedView.All;
    public bool ViewStarred => _selectedView == SelectedView.Starred;
    public bool ViewTagged => _selectedView == SelectedView.Tagged;
}