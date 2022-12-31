using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model.Home;

public class LoginViewModel : BaseViewModel
{
    public string ReturnUrl { get; }
    public LoginViewModel(HttpContext context, string returnUrl) : base(context, SelectedView.None) => ReturnUrl = returnUrl;
}