using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model.Home;

public class LoginVerifyViewModel(HttpContext context, string? returnUrl, string email,
    bool isReturningUser, string verifyOptions)
    : BaseViewModel(context, SelectedView.None)
{
    public string? ReturnUrl { get; } = returnUrl;
    public string Email { get; } = email;
    public bool IsReturningUser { get; } = isReturningUser;
    public string VerifyOptions { get; } = verifyOptions;
}