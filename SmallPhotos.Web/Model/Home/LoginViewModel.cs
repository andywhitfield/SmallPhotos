using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model.Home;

public class LoginViewModel(HttpContext context, string? returnUrl) : BaseViewModel(context, SelectedView.None)
{
    public string? ReturnUrl { get; } = returnUrl;
}