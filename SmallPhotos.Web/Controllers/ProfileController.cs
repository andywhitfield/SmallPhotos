using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallPhotos.Model;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;
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
            return View(new IndexViewModel(HttpContext, response.Folders, response.ThumbnailSize, response.GalleryImagePageSize));
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
        public async Task<IActionResult> DeleteFolder([FromRoute, Required]long albumSourceId)
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

        [HttpPost("~/profile/thumbnailsize")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThumnailSize([FromForm, Required, ModelBinder(Name="thumbnail-size-selector")]int thumbnailSize)
        {
            if (!ModelState.IsValid || !Enum.IsDefined(typeof(ThumbnailSize), thumbnailSize))
                return BadRequest();

            var headers = Request.GetTypedHeaders();
            var uriReferer = headers.Referer ?? new("~/");
            var updateThumbnailSize = (ThumbnailSize)thumbnailSize;
            if (!await _mediator.Send(new UpdateUserThumbnailSizeRequest(User, updateThumbnailSize)))
                return BadRequest();

            _logger.LogDebug($"Updated user thumbnail size to {updateThumbnailSize} - redirect to [{uriReferer}]");
            return Redirect(uriReferer.ToString());
        }

        [HttpPost("~/profile/viewoptions")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveViewOptions([FromForm, Required]int thumbnailSize, [FromForm, Required]int pageSize)
        {
            if (!ModelState.IsValid || !Enum.IsDefined(typeof(ThumbnailSize), thumbnailSize) || pageSize < 1 || pageSize > Pagination.MaxPageSize)
                return BadRequest();

            var updateThumbnailSize = (ThumbnailSize)thumbnailSize;
            if (!await _mediator.Send(new SaveViewOptionsRequest(User, updateThumbnailSize, pageSize)))
                return BadRequest();

            _logger.LogDebug($"Updated user thumbnail size to {updateThumbnailSize} and page size to {pageSize}");
            return Redirect("~/profile");
        }
    }
}