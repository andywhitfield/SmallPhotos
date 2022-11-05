namespace SmallPhotos.Web.Views.Shared.Components;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

public class NavigationViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync() => Task.FromResult((IViewComponentResult)View());
}
