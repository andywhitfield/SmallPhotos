using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;
using SmallPhotos.Web.Model.Map;

namespace SmallPhotos.Web.Controllers;

[Authorize]
public class MapController(IMediator mediator)
    : Controller
{
    [HttpPost("~/map")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([FromForm, Required] string view, [FromForm, Required] string photoIds)
    {
        var selectedView = Enum.TryParse<SelectedView>(view, out var v) ? v : default(SelectedView?);
        if (selectedView == null)
            return BadRequest();

        var response = await mediator.Send(new MapPageRequest(User, photoIds));
        if (!response.IsUserValid)
            return Redirect("~/signin");

        return View(new IndexViewModel(HttpContext, response.Photos, selectedView.Value));
    }
}
