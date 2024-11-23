using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model.Home;

public class ErrorViewModel(HttpContext context) : BaseViewModel(context, SelectedView.None)
{
}