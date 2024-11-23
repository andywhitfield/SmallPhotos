using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;
using SmallPhotos.Web.Model.Home;

namespace SmallPhotos.Web.Controllers;

public class HomeController(ILogger<HomeController> logger, IMediator mediator)
    : Controller
{
    [Authorize]
    [HttpGet("~/")]
    public async Task<IActionResult> Index([FromQuery]int? photoId = null, [FromQuery]int? pageNumber = null)
    {           
        var response = await mediator.Send(new HomePageRequest(User, pageNumber ?? 1, photoId));
        if (!response.IsUserValid)
            return Redirect("~/signin");

        return View(new IndexViewModel(HttpContext, response.ThumbnailSize, response.Photos, response.Pagination, SelectedView.All));
    }

    public IActionResult Error() => View(new ErrorViewModel(HttpContext));

    [HttpGet("~/signin")]
    public IActionResult Signin([FromQuery] string? returnUrl) => View("Login", new LoginViewModel(HttpContext, returnUrl));

    [HttpPost("~/signin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Signin([FromForm] string? returnUrl, [FromForm, Required] string email)
    {
        if (!ModelState.IsValid)
            return View("Login", new LoginViewModel(HttpContext, returnUrl));

        var response = await mediator.Send(new SigninRequest(email));
        return View("LoginVerify", new LoginVerifyViewModel(HttpContext, returnUrl, email,
            response.IsReturningUser, response.VerifyOptions));
    }

    [HttpPost("~/signin/verify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SigninVerify(
        [FromForm] string? returnUrl,
        [FromForm, Required] string email,
        [FromForm, Required] string verifyOptions,
        [FromForm, Required] string verifyResponse)
    {
        if (!ModelState.IsValid)
            return Redirect("~/signin");

        var isValid = await mediator.Send(new SigninVerifyRequest(HttpContext, email, verifyOptions, verifyResponse));
        if (isValid)
        {
            var redirectUri = "~/";
            if (!string.IsNullOrEmpty(returnUrl) && Uri.TryCreate(returnUrl, UriKind.Relative, out var uri))
                redirectUri = uri.ToString();

            return Redirect(redirectUri);
        }
        
        logger.LogWarning("Signin failed, redirecting to initial signin page");
        return Redirect("~/signin");
    }

    [HttpPost("~/signout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Signout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("~/");
    }
}