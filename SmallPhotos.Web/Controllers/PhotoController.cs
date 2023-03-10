using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Controllers;

[Authorize]
public class PhotoController : Controller
{
    private readonly ILogger<PhotoController> _logger;
    private readonly IMediator _mediator;

    public PhotoController(ILogger<PhotoController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet("~/photo/thumbnail/{size}/{photoId}/{filename}")]
    public async Task<IActionResult> Thumbnail(string size, string photoId, string filename)
    {
        if (!long.TryParse(photoId, out var photoIdValue))
            return NotFound();

        _logger.LogInformation($"Getting image {photoId}, size {size}");

        var response = await _mediator.Send(new GetPhotoRequest(User, photoIdValue, filename, size ?? "", false));
        if (response?.ImageStream == null || response.ImageContentType == null)
            return NotFound();

        return File(response.ImageStream, response.ImageContentType, filename, new DateTimeOffset(DateTime.UtcNow), EntityTagHeaderValue.Any, false);
    }

    [HttpGet("~/photo/{photoId}/{filename}")]
    public Task<IActionResult> Photo(string photoId, string filename) => Photo(photoId, filename, false);

    [HttpGet("~/photo/original/{photoId}/{filename}")]
    public Task<IActionResult> Original(string photoId, string filename) => Photo(photoId, filename, true);

    private async Task<IActionResult> Photo(string photoId, string filename, bool original)
    {
        if (!long.TryParse(photoId, out var photoIdValue))
            return NotFound();

        _logger.LogInformation($"Getting image {photoId}");

        var response = await _mediator.Send(new GetPhotoRequest(User, photoIdValue, filename, null, original));
        if (response?.ImageStream == null || response.ImageContentType == null)
            return NotFound();

        return File(response.ImageStream, response.ImageContentType, filename, new DateTimeOffset(DateTime.UtcNow), EntityTagHeaderValue.Any, false);
    }
}