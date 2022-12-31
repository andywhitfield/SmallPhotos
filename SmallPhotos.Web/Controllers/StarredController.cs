using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;
using SmallPhotos.Web.Model.Home;

namespace SmallPhotos.Web.Controllers;

[Authorize]
public class StarredController : Controller
{
    private readonly ILogger<StarredController> _logger;
    private readonly IMediator _mediator;

    public StarredController(ILogger<StarredController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet("~/starred")]
    public async Task<IActionResult> Index([FromQuery] int? photoId = null, [FromQuery] int? pageNumber = null)
    {
        var response = await _mediator.Send(new HomePageRequest(User, pageNumber ?? 1, photoId, true));
        if (!response.IsUserValid)
            return Redirect("~/signin");

        return View(new IndexViewModel(HttpContext, response.ThumbnailSize, response.Photos, response.Pagination, SelectedView.Starred));
    }
}