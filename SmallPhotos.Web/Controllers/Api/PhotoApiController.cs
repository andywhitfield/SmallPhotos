using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Web.Model.Gallery;

namespace SmallPhotos.Web.Controllers.Api;

[ApiController, Authorize, Route("api/[controller]")]
public class PhotoApiController(ILogger<PhotoApiController> logger, IUserAccountRepository userAccountRepository,
    IPhotoRepository photoRepository)
    : ControllerBase
{
    [HttpPost("star/{photoId}")]
    public Task<ActionResult> Star(long photoId) => StarUnstar(photoId, true);

    [HttpPost("unstar/{photoId}")]
    public Task<ActionResult> Unstar(long photoId) => StarUnstar(photoId, false);

    [HttpPost("tag/{photoId}")]
    public async Task<ActionResult> Tag(long photoId, AddTagRequest addTagRequest)
    {
        logger.LogInformation("Adding tag to photo {PhotoId}: {AddTagRequest.Tag}", photoId, addTagRequest.Tag);
        if (string.IsNullOrWhiteSpace(addTagRequest.Tag))
            return BadRequest();

        var user = await userAccountRepository.GetUserAccountOrNullAsync(User);
        if (user == null)
            return BadRequest();

        var photo = await photoRepository.GetAsync(user, photoId);
        if (photo == null)
            return BadRequest();

        await photoRepository.AddTagAsync(user, photo, addTagRequest.Tag);
        return Ok();
    }

    [HttpDelete("tag/{photoId}")]
    public async Task<ActionResult> ClearAllTags(long photoId)
    {
        logger.LogInformation("Clearing all tags on photo {PhotoId}", photoId);
        var user = await userAccountRepository.GetUserAccountOrNullAsync(User);
        if (user == null)
            return BadRequest();

        var photo = await photoRepository.GetAsync(user, photoId);
        if (photo == null)
            return BadRequest();

        await photoRepository.DeleteTagsAsync(user, photo);
        return Ok();
    }

    private async Task<ActionResult> StarUnstar(long photoId, bool star)
    {
        logger.LogInformation("Starring / Unstarring photo: {PhotoId} - {Star}", photoId, star);

        var user = await userAccountRepository.GetUserAccountOrNullAsync(User);
        if (user == null)
            return BadRequest();

        var photo = await photoRepository.GetAsync(user, photoId);
        if (photo == null)
            return BadRequest();

        await (star ? photoRepository.StarAsync(user, photo) : photoRepository.UnstarAsync(user, photo));
        return Ok();
    }
}