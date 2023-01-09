using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Dropbox.Api;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmallPhotos.Dropbox;
using SmallPhotos.Model;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;
using SmallPhotos.Web.Model.Profile;

namespace SmallPhotos.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ILogger<ProfileController> _logger;
    private readonly IMediator _mediator;
    private readonly string _dropboxAppKey;
    private readonly string _dropboxAppSecret;

    public ProfileController(ILogger<ProfileController> logger, IMediator mediator, IOptions<DropboxOptions> dropboxConfig)
    {
        _logger = logger;
        _mediator = mediator;
        _dropboxAppKey = dropboxConfig.Value.SmallPhotosAppKey ?? "";
        _dropboxAppSecret = dropboxConfig.Value.SmallPhotosAppSecret ?? "";
    }

    [HttpGet("~/profile")]
    public async Task<IActionResult> Index()
    {
        var response = await _mediator.Send(new GetProfileRequest(User));
        return View(new IndexViewModel(HttpContext, response.Folders, response.ThumbnailSize, response.GalleryImagePageSize));
    }

    [HttpPost("~/profile/folder/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFolder([FromForm, Required] string folder, [FromForm] bool? folderRecursive)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogInformation("Model state is invalid, returning bad request");
            return BadRequest();
        }

        var added = await _mediator.Send(new AddSourceFolderRequest(User, folder, folderRecursive ?? false));
        if (!added)
            return BadRequest();

        return Redirect("~/profile");
    }

    [HttpPost("~/profile/folder/delete/{albumSourceId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFolder([FromRoute, Required] long albumSourceId)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogInformation("Model state is invalid, returning bad request");
            return BadRequest();
        }

        var deleted = await _mediator.Send(new DeleteSourceFolderRequest(User, albumSourceId));
        if (!deleted)
            return BadRequest();

        return Redirect("~/profile");
    }

    [HttpPost("~/profile/thumbnailsize")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThumnailSize([FromForm, Required, ModelBinder(Name = "thumbnail-size-selector")] int thumbnailSize)
    {
        if (!ModelState.IsValid || !Enum.IsDefined(typeof(ThumbnailSize), thumbnailSize))
            return BadRequest();

        var headers = Request.GetTypedHeaders();
        var uriReferer = headers.Referer ?? new("~/");
        var updateThumbnailSize = (ThumbnailSize)thumbnailSize;
        if (!await _mediator.Send(new UpdateUserThumbnailSizeRequest(User, updateThumbnailSize)))
            return BadRequest();

        _logger.LogDebug($"Updated user thumbnail size to {updateThumbnailSize} - redirect to [{uriReferer}]");
        return Redirect(uriReferer.ToString());
    }

    [HttpPost("~/profile/viewoptions")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveViewOptions([FromForm, Required] int thumbnailSize, [FromForm, Required] int pageSize)
    {
        if (!ModelState.IsValid || !Enum.IsDefined(typeof(ThumbnailSize), thumbnailSize) || pageSize < 1 || pageSize > Pagination.MaxPageSize)
            return BadRequest();

        var updateThumbnailSize = (ThumbnailSize)thumbnailSize;
        if (!await _mediator.Send(new SaveViewOptionsRequest(User, updateThumbnailSize, pageSize)))
            return BadRequest();

        _logger.LogDebug($"Updated user thumbnail size to {updateThumbnailSize} and page size to {pageSize}");
        return Redirect("~/profile");
    }

    [HttpPost("~/profile/dropbox/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DropboxAddFolder([FromForm] string? folder, [FromForm] bool? folderRecursive, [FromForm] string? accessToken, [FromForm] string? refreshToken)
    {
        if (!string.IsNullOrEmpty(folder) && !string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
        {
            var added = await _mediator.Send(new AddSourceFolderRequest(User, folder, folderRecursive ?? false, accessToken, refreshToken));
            if (!added)
                return BadRequest();

            return Redirect("~/profile");
        }

        var dropboxRedirect = DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Code, _dropboxAppKey, RedirectUri, tokenAccessType: TokenAccessType.Offline, scopeList: new[] { "files.content.read" });
        _logger.LogInformation($"Getting user token from Dropbox: {dropboxRedirect} (redirect={RedirectUri})");
        return Redirect(dropboxRedirect.ToString());
    }

    [HttpGet("~/dropbox-authentication")]
    [Authorize]
    public async Task<ActionResult> DropboxAuthentication(string code, string state)
    {
        var response = await DropboxOAuth2Helper.ProcessCodeFlowAsync(code, _dropboxAppKey, _dropboxAppSecret, RedirectUri.ToString());
        _logger.LogInformation($"Got user tokens from Dropbox: {response.AccessToken} / {response.RefreshToken}");

        var profileResponse = await _mediator.Send(new GetProfileRequest(User));
        return View("Index", new IndexViewModel(HttpContext, profileResponse.Folders, profileResponse.ThumbnailSize, profileResponse.GalleryImagePageSize, response.AccessToken, response.RefreshToken));
    }

    private Uri RedirectUri
    {
        get
        {
            UriBuilder uriBuilder = new();
            uriBuilder.Scheme = Request.Scheme;
            uriBuilder.Host = Request.Host.Host;
            if (Request.Host.Port.HasValue && Request.Host.Port != 443 && Request.Host.Port != 80)
                uriBuilder.Port = Request.Host.Port.Value;
            uriBuilder.Path = "dropbox-authentication";
            return uriBuilder.Uri;
        }
    }
}