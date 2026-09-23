using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;
using SmallPhotos.Web.Model.Home;

namespace SmallPhotos.Web.Controllers;

[Authorize]
public class ReminisceController(IMediator mediator)
    : Controller
{
    [HttpGet("~/reminisce")]
    public async Task<IActionResult> Index()
    {
        var response = await mediator.Send(new ReminiscePageRequest(User));
        if (!response.IsUserValid)
            return Redirect("~/signin");

        return View(new IndexViewModel(HttpContext, response.ThumbnailSize, response.Photos, Pagination.Empty,
            response.ShowDetails, SelectedView.Reminisce, default, default));
    }
}