using System.Threading.Tasks;
using System.Web;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallPhotos.Model;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model.Home;

namespace SmallPhotos.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMediator _mediator;

        public HomeController(IMediator mediator) => _mediator = mediator;

        [Authorize]
        [HttpGet("~/")]
        public async Task<IActionResult> Index()
        {
            var thumbnailSize = ThumbnailSize.Small;
            var response = await _mediator.Send(new HomePageRequest(User, thumbnailSize));
            if (!response.IsUserValid)
                return Redirect("~/signin");

            return View(new IndexViewModel(HttpContext, thumbnailSize, response.Photos));
        }

        public IActionResult Error() => View(new ErrorViewModel(HttpContext));

        [HttpGet("~/signin")]
        public IActionResult Signin([FromQuery] string returnUrl) => View("Login", new LoginViewModel(HttpContext, returnUrl));

        [HttpPost("~/signin")]
        [ValidateAntiForgeryToken]
        public IActionResult SigninChallenge([FromForm] string returnUrl) => Challenge(new AuthenticationProperties { RedirectUri = $"/signedin?returnUrl={HttpUtility.UrlEncode(returnUrl)}" }, OpenIdConnectDefaults.AuthenticationScheme);

        [Authorize]
        [HttpGet("~/signedin")]
        public async Task<IActionResult> SignedIn([FromQuery] string returnUrl) => Redirect(await _mediator.Send(new SignedInRequest(User, returnUrl)));

        [HttpPost("~/signout")]
        [ValidateAntiForgeryToken]
        public IActionResult Signout()
        {
            HttpContext.Session.Clear();
            return SignOut(CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}