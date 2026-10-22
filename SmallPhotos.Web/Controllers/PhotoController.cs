using System.ComponentModel.DataAnnotations;
using System.Globalization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Controllers;

[Authorize]
public class PhotoController(ILogger<PhotoController> logger, IMediator mediator)
    : Controller
{
    [HttpGet("~/photo/thumbnail/{size}/{photoId}/{filename}"), HttpHead("~/photo/thumbnail/{size}/{photoId}/{filename}")]
    public async Task<IActionResult> Thumbnail([Required] string size, [Required] string photoId, [Required] string filename)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        if (!long.TryParse(photoId, out var photoIdValue))
            return NotFound();

        logger.LogInformation("Getting image {PhotoId}, size {Size}", photoId, size);
        return await GetPhotoResultAsync(await mediator.Send(new GetPhotoRequest(User, photoIdValue, filename, size ?? "", false)));
    }

    [HttpGet("~/photo/{photoId}/{filename}"), HttpHead("~/photo/{photoId}/{filename}")]
    public Task<IActionResult> Photo([Required] string photoId, [Required] string filename) => Photo(photoId, filename, false);

    [HttpGet("~/photo/original/{photoId}/{filename}"), HttpHead("~/photo/original/{photoId}/{filename}")]
    public Task<IActionResult> Original([Required] string photoId, [Required] string filename) => Photo(photoId, filename, true);

    private async Task<IActionResult> Photo(string photoId, string filename, bool original)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        if (!long.TryParse(photoId, out var photoIdValue))
            return NotFound();

        logger.LogInformation("Getting image {PhotoId}", photoId);
        return await GetPhotoResultAsync(await mediator.Send(new GetPhotoRequest(User, photoIdValue, filename, null, original)));
    }

    private async Task<IActionResult> GetPhotoResultAsync(GetPhotoResponse? response)
    {
        if (response?.ImageStream == null || response.ImageContentType == null || response.ImageLastModified == null)
            return NotFound();

        if (Request.Headers.IfNoneMatch.ToString() == response.ImageETag)
            return StatusCode(StatusCodes.Status304NotModified);

        if (DateTime.TryParseExact(Request.Headers.IfModifiedSince.ToString(), "r", null, DateTimeStyles.RoundtripKind, out var ifModifiedSince) &&
            response.ImageLastModified <= ifModifiedSince)
            return StatusCode(StatusCodes.Status304NotModified);

        Response.StatusCode = StatusCodes.Status200OK;
        Response.Headers.ContentLength = response.ImageStream.Length;
        Response.Headers.ContentType = response.ImageContentType;
        Response.Headers.LastModified = response.ImageLastModified.Value.ToString("r");
        Response.Headers.ETag = response.ImageETag;
        if (Request.Method == HttpMethods.Get)
            await response.ImageStream.CopyToAsync(Response.Body);

        return Empty;
    }
}