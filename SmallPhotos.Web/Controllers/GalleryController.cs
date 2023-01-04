using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model.Gallery;

namespace SmallPhotos.Web.Controllers;

[Authorize]
public class GalleryController : Controller
{
    private readonly IMediator _mediator;

    public GalleryController(IMediator mediator) => _mediator = mediator;

    [HttpGet("~/gallery/{photoId}/{photoFilename}")]
    public async Task<IActionResult> Index(string photoId, string photoFilename, [FromQuery] string? from)
    {
        if (!long.TryParse(photoId, out var photoIdValue))
            return NotFound();

        var response = await _mediator.Send(new GalleryRequest(User, photoIdValue, photoFilename, from == "starred", (from ?? "").StartsWith("tagged_") ? from!.Substring("tagged_".Length) : ""));
        if (response.Photo == null)
            return NotFound();

        return View(new IndexViewModel(HttpContext, response.Photo, response.PreviousPhoto, response.NextPhoto, response.PhotoNumber, response.PhotoCount, from));
    }
}