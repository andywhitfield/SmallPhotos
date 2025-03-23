using System.ComponentModel.DataAnnotations;
using Dropbox.Api;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SmallPhotos.Dropbox;
using SmallPhotos.Model;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;
using SmallPhotos.Web.Model.Profile;

namespace SmallPhotos.Web.Controllers;

[Authorize]
public class ProfileController(ILogger<ProfileController> logger, IMediator mediator,
    IOptions<DropboxOptions> dropboxConfig)
    : Controller
{
    private readonly string _dropboxAppKey = dropboxConfig.Value.SmallPhotosAppKey ?? "";
    private readonly string _dropboxAppSecret = dropboxConfig.Value.SmallPhotosAppSecret ?? "";

    [HttpGet("~/profile")]
    public async Task<IActionResult> Index()
    {
        var response = await mediator.Send(new GetProfileRequest(User));
        return View(new IndexViewModel(HttpContext, response.Folders, response.ThumbnailSize, response.GalleryImagePageSize));
    }

    [HttpPost("~/profile/folder/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFolder([FromForm, Required] string folder, [FromForm] bool? folderRecursive)
    {
        if (!ModelState.IsValid)
        {
            logger.LogInformation("Model state is invalid, returning bad request");
            return BadRequest();
        }

        var added = await mediator.Send(new AddSourceFolderRequest(User, folder, folderRecursive ?? false));
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
            logger.LogInformation("Model state is invalid, returning bad request");
            return BadRequest();
        }

        var deleted = await mediator.Send(new DeleteSourceFolderRequest(User, albumSourceId));
        if (!deleted)
            return BadRequest();

        return Redirect("~/profile");
    }

    [HttpPost("~/profile/thumbnailsize")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThumbnailSize([FromForm, Required, ModelBinder(Name = "thumbnail-size-selector")] int thumbnailSize)
    {
        if (!ModelState.IsValid || !Enum.IsDefined(typeof(ThumbnailSize), thumbnailSize))
            return BadRequest();

        var headers = Request.GetTypedHeaders();
        var uriReferer = headers.Referer ?? new("~/");
        var updateThumbnailSize = (ThumbnailSize)thumbnailSize;
        if (!await mediator.Send(new UpdateUserThumbnailSizeRequest(User, updateThumbnailSize)))
            return BadRequest();

        logger.LogDebug("Updated user thumbnail size to {UpdateThumbnailSize} - redirect to [{UriReferer}]", updateThumbnailSize, uriReferer);
        return Redirect(uriReferer.ToString());
    }

    [HttpPost("~/profile/thumbnaildetails")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThumbnailDetails([FromForm, ModelBinder(Name = "thumbnail-details")] string? thumbnailDetails)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var headers = Request.GetTypedHeaders();
        var uriReferer = headers.Referer ?? new("~/");
        if (!await mediator.Send(new UpdateUserThumbnailDetailsRequest(User, !string.IsNullOrEmpty(thumbnailDetails))))
            return BadRequest();

        logger.LogDebug("Updated user thumbnail details to {ThumbnailDetails} - redirect to [{UriReferer}]", !string.IsNullOrEmpty(thumbnailDetails), uriReferer);
        return Redirect(uriReferer.ToString());
    }

    [HttpPost("~/profile/viewoptions")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveViewOptions([FromForm, Required] int thumbnailSize, [FromForm, Required] int pageSize)
    {
        if (!ModelState.IsValid || !Enum.IsDefined(typeof(ThumbnailSize), thumbnailSize) || pageSize < 1 || pageSize > Pagination.MaxPageSize)
            return BadRequest();

        var updateThumbnailSize = (ThumbnailSize)thumbnailSize;
        if (!await mediator.Send(new SaveViewOptionsRequest(User, updateThumbnailSize, pageSize)))
            return BadRequest();

        logger.LogDebug("Updated user thumbnail size to {UpdateThumbnailSize} and page size to {PageSize}", updateThumbnailSize, pageSize);
        return Redirect("~/profile");
    }

    [HttpPost("~/profile/dropbox/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DropboxAddFolder([FromForm] string? folder, [FromForm] bool? folderRecursive, [FromForm] string? accessToken, [FromForm] string? refreshToken)
    {
        if (!string.IsNullOrEmpty(folder) && !string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
        {
            var added = await mediator.Send(new AddSourceFolderRequest(User, folder, folderRecursive ?? false, accessToken, refreshToken));
            if (!added)
                return BadRequest();

            return Redirect("~/profile");
        }

        var dropboxRedirect = DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Code, _dropboxAppKey, RedirectUri, tokenAccessType: TokenAccessType.Offline, scopeList: new[] { "files.content.read" });
        logger.LogInformation("Getting user token from Dropbox: {DropboxRedirect} (redirect={RedirectUri})", dropboxRedirect, RedirectUri);
        return Redirect(dropboxRedirect.ToString());
    }

    [HttpGet("~/dropbox-authentication")]
    [Authorize]
    public async Task<ActionResult> DropboxAuthentication(string code, string state)
    {
        var response = await DropboxOAuth2Helper.ProcessCodeFlowAsync(code, _dropboxAppKey, _dropboxAppSecret, RedirectUri.ToString());
        logger.LogInformation("Got user tokens from Dropbox: {AccessToken} / {RefreshToken}", response.AccessToken, response.RefreshToken);

        var profileResponse = await mediator.Send(new GetProfileRequest(User));
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