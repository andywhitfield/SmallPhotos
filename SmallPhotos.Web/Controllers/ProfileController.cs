using System.ComponentModel.DataAnnotations;
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
            var response = await _mediator.Send(new GetProfileRequest(User));
            return View(new IndexViewModel(HttpContext, response.Folders));
        }

        [HttpPost("~/profile/folder/add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFolder([FromForm, Required]string folder)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogInformation("Model state is invalid, returning bad request");
                return BadRequest();
            }

            var added = await _mediator.Send(new AddSourceFolderRequest(User, folder));
            if (!added)
                return BadRequest();

            return Redirect("~/profile");
        }

        [HttpPost("~/profile/folder/delete/{albumSourceId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFolder([FromRoute, Required]int albumSourceId)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogInformation("Model state is invalid, returning bad request");
                return BadRequest();
            }

            var deleted = await _mediator.Send(new DeleteSourceFolderRequest(User, albumSourceId));
            if (!deleted)
                return BadRequest();

            return Redirect("~/profile");
        }
    }
}