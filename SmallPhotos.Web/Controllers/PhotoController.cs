using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Controllers
{
    [Authorize]
    public class PhotoController : Controller
    {
        private readonly IMediator _mediator;

        public PhotoController(IMediator mediator) => _mediator = mediator;

        [HttpGet("~/photo/{photoId}/{filename}")]
        public async Task<IActionResult> Index(string photoId, string filename)
        {
            if (!long.TryParse(photoId, out var photoIdValue))
                return NotFound();

            var response = await _mediator.Send(new GetPhotoRequest(User, photoIdValue, filename));
            if (response?.ImageStream == null)
                return NotFound();

            return File(response.ImageStream, "image/jpeg", filename, new DateTimeOffset(DateTime.UtcNow), EntityTagHeaderValue.Any, false);
        }
    }
}