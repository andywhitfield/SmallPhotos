using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model.Profile;

namespace SmallPhotos.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ILogger<ProfileController> _logger;
        private readonly IMediator _mediator;

        public ProfileController(ILogger<ProfileController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet("~/profile")]
        public async Task<IActionResult> Index()
        {
            var response = await _mediator.Send(new GetProfileRequest());
            return View(new IndexViewModel(HttpContext));
        }
    }
}