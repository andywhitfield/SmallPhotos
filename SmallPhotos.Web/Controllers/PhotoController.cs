using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Controllers
{
    [Authorize]
    public class PhotoController : Controller
    {
        private readonly ILogger<PhotoController> _logger;
        private readonly IMediator _mediator;

        public PhotoController(ILogger<PhotoController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet("~/photo/{photoId}/{filename}")]
        public async Task<IActionResult> Index(string photoId, string filename, [FromQuery] string size = null)
        {
            if (!long.TryParse(photoId, out var photoIdValue))
                return NotFound();

            _logger.LogInformation($"Getting image {photoId}, size {size}");

            var response = await _mediator.Send(new GetPhotoRequest(User, photoIdValue, filename, size));
            if (response?.ImageStream == null)
                return NotFound();

            return File(response.ImageStream, response.ImageContentType, filename, new DateTimeOffset(DateTime.UtcNow), EntityTagHeaderValue.Any, false);
        }
    }
}