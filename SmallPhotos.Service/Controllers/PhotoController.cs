using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Service.Models;

namespace SmallPhotos.Service.Controllers
{
    [ApiController, Route("api/[controller]")]
    public class PhotoController : ControllerBase
    {
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IAlbumRepository _albumRepository;
        private readonly IPhotoRepository _photoRepository;

        public PhotoController(
            IUserAccountRepository userAccountRepository,
            IAlbumRepository albumRepository,
            IPhotoRepository photoRepository)
        {
            _userAccountRepository = userAccountRepository;
            _albumRepository = albumRepository;
            _photoRepository = photoRepository;
        }

        [HttpPost]
        public async Task<ActionResult<Photo>> Post(CreateOrUpdatePhotoRequest request)
        {
            var userAccount = await _userAccountRepository.GetAsync(request.UserAccountId);
            if (userAccount == null)
                return BadRequest();

            var albumSource = await _albumRepository.GetAsync(userAccount, request.AlbumSourceId);
            if (albumSource == null)
                return BadRequest();

            var file = new FileInfo(Path.Join(albumSource.Folder, request.Filename));
            if (!file.Exists)
                return BadRequest();

            var photo = await _photoRepository.GetAsync(userAccount, albumSource, request.Filename);
            if (photo == null)
            {
                photo = await _photoRepository.AddAsync(albumSource, file);
            }
            else
            {
                await _photoRepository.UpdateAsync(photo, file);
            }

            return photo;
        }
    }
}