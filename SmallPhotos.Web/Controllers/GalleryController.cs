using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model.Gallery;

namespace SmallPhotos.Web.Controllers
{
    [Authorize]
    public class GalleryController : Controller
    {
        private readonly IMediator _mediator;

        public GalleryController(IMediator mediator) => _mediator = mediator;

        [HttpGet("~/gallery/{photoId}/{photoFilename}")]
        public async Task<IActionResult> Index(string photoId, string photoFilename)
        {
            if (!long.TryParse(photoId, out var photoIdValue))
                return NotFound();

            var response = await _mediator.Send(new GalleryRequest(User, photoIdValue, photoFilename));
            if (response.Photo == null)
                return NotFound();

            return View(new IndexViewModel(HttpContext, response.Photo, null, null));
        }
    }
}