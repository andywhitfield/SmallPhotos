using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;
using SmallPhotos.Web.Model.Tagged;

namespace SmallPhotos.Web.Controllers;

[Authorize]
public class TaggedController(IMediator mediator) : Controller
{
    [HttpGet("~/tagged")]
    public async Task<IActionResult> Index() =>
        View(new IndexViewModel(HttpContext, await mediator.Send(new TaggedPhotoHomeRequest(User))));

    [HttpGet("~/tagged/{tag}")]
    public async Task<IActionResult> PhotosWithTag(string tag, [FromQuery] int? photoId = null,
        [FromQuery] int? pageNumber = null, [FromQuery] string? fromDate = null, [FromQuery] string? toDate = null)
    {
        var response = await mediator.Send(new HomePageRequest(User, pageNumber ?? 1, photoId, withTag: tag,
            fromDate: fromDate, toDate: toDate));
        if (!response.IsUserValid)
            return Redirect("~/signin");

        return View(new Model.Home.IndexViewModel(HttpContext, response.ThumbnailSize, response.Photos,
            response.Pagination, response.ShowDetails, SelectedView.Tagged, response.MinFilterDate,
            response.MaxFilterDate, withTag: tag, filterFromDate: response.FilterFromDate,
            filterToDate: response.FilterToDate));
    }
}