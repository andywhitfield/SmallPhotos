using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;
using SmallPhotos.Web.Model.Home;

namespace SmallPhotos.Web.Controllers;

[Authorize]
public class StarredController(IMediator mediator)
    : Controller
{
    [HttpGet("~/starred")]
    public async Task<IActionResult> Index([FromQuery] int? photoId = null, [FromQuery] int? pageNumber = null)
    {
        var response = await mediator.Send(new HomePageRequest(User, pageNumber ?? 1, photoId, true));
        if (!response.IsUserValid)
            return Redirect("~/signin");

        return View(new IndexViewModel(HttpContext, response.ThumbnailSize, response.Photos, response.Pagination, response.ShowDetails, SelectedView.Starred));
    }
}