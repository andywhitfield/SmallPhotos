using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;
using SmallPhotos.Web.Model.Tagged;

namespace SmallPhotos.Web.Controllers;

[Authorize]
public class TaggedController : Controller
{
    private readonly ILogger<TaggedController> _logger;
    private readonly IMediator _mediator;

    public TaggedController(ILogger<TaggedController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet("~/tagged")]
    public async Task<IActionResult> Index() =>
        View(new IndexViewModel(HttpContext, await _mediator.Send(new TaggedPhotoHomeRequest(User))));
    
    [HttpGet("~/tagged/{tag}")]
    public async Task<IActionResult> PhotosWithTag(string tag, [FromQuery] int? photoId = null, [FromQuery] int? pageNumber = null)
    {
        var response = await _mediator.Send(new HomePageRequest(User, pageNumber ?? 1, photoId, withTag: tag));
        if (!response.IsUserValid)
            return Redirect("~/signin");

        return View(new SmallPhotos.Web.Model.Home.IndexViewModel(HttpContext, response.ThumbnailSize, response.Photos, response.Pagination, SelectedView.Tagged, withTag: tag));
    }
}