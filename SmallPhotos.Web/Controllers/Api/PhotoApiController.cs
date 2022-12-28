using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallPhotos.Data;

namespace SmallPhotos.Web.Controllers.Api;

[ApiController, Authorize, Route("api/[controller]")]
public class PhotoApiController : ControllerBase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IPhotoRepository _photoRepository;

    public PhotoApiController(IUserAccountRepository userAccountRepository, IPhotoRepository photoRepository)
    {
        _userAccountRepository = userAccountRepository;
        _photoRepository = photoRepository;
    }

    [HttpPost("star/{photoId}")]
    public Task<ActionResult> Star(long photoId) => StarUnstar(photoId, true);

    [HttpPost("unstar/{photoId}")]
    public Task<ActionResult> Unstar(long photoId) => StarUnstar(photoId, false);

    private async Task<ActionResult> StarUnstar(long photoId, bool star)
    {
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