using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Web.Model.Gallery;

namespace SmallPhotos.Web.Controllers.Api;

[ApiController, Authorize, Route("api/[controller]")]
public class PhotoApiController : ControllerBase
{
    private readonly ILogger<PhotoApiController> _logger;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IPhotoRepository _photoRepository;

    public PhotoApiController(ILogger<PhotoApiController> logger, IUserAccountRepository userAccountRepository, IPhotoRepository photoRepository)
    {
        _logger = logger;
        _userAccountRepository = userAccountRepository;
        _photoRepository = photoRepository;
    }

    [HttpPost("star/{photoId}")]
    public Task<ActionResult> Star(long photoId) => StarUnstar(photoId, true);

    [HttpPost("unstar/{photoId}")]
    public Task<ActionResult> Unstar(long photoId) => StarUnstar(photoId, false);

    [HttpPost("tag/{photoId}")]
    public ActionResult Tag(long photoId, AddTagRequest addTagRequest)
    {
        _logger.LogInformation($"Adding tag to photo {photoId}: {addTagRequest.Tag}");
        return Ok();
    }

    [HttpDelete("tag/{photoId}")]
    public ActionResult ClearAllTags(long photoId)
    {
        _logger.LogInformation($"Clearing all tags on photo {photoId}");
        return Ok();
    }

    private async Task<ActionResult> StarUnstar(long photoId, bool star)
    {
        _logger.LogInformation($"Starring / Unstarring photo: {photoId} - {star}");

        var user = await _userAccountRepository.GetUserAccountOrNullAsync(User);
        if (user == null)
            return BadRequest();

        var photo = await _photoRepository.GetAsync(user, photoId);
        if (photo == null)
            return BadRequest();

        await (star ? _photoRepository.StarAsync(user, photo) : _photoRepository.UnstarAsync(user, photo));
        return Ok();
    }
}