using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;
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
                return BadRequest($"User [{request.UserAccountId}] not found");

            var albumSource = await _albumRepository.GetAsync(userAccount, request.AlbumSourceId);
            if (albumSource == null)
                return BadRequest($"Album [{request.AlbumSourceId}] not found");

            var file = new FileInfo(Path.Join(albumSource.Folder, request.Filename));
            if (!file.Exists)
                return BadRequest($"File [{request.Filename}] does not exist");

            var format = file.Extension.ToLowerInvariant() switch
            {
                ".jpg" => MagickFormat.Jpg,
                ".jpeg" => MagickFormat.Jpeg,
                ".gif" => MagickFormat.Gif,
                ".heic" => MagickFormat.Heic,
                _ => MagickFormat.Unknown
            };
            if (format == MagickFormat.Unknown)
                return BadRequest($"Unknown image format: [{file.Extension}]");

            using var image = new MagickImage(Path.Combine(albumSource.Folder, file.Name), format);
            var originalSize = new Size(image.Width, image.Height);

            var photo = await _photoRepository.GetAsync(userAccount, albumSource, request.Filename);
            if (photo == null)
                photo = await _photoRepository.AddAsync(albumSource, file, originalSize);
            else
                await _photoRepository.UpdateAsync(photo, file, originalSize);

            foreach (var thumbnailSize in Enum.GetValues<ThumbnailSize>())
                await SaveThumbnail(photo, image.Clone(), thumbnailSize);

            return photo;
        }

        private async Task SaveThumbnail(Photo photo, IMagickImage image, ThumbnailSize thumbnailSize)
        {
            using var jpegStream = new MemoryStream();
            var resizeTo = thumbnailSize.ToSize();
            var geometry = new MagickGeometry(resizeTo.Width, resizeTo.Height) { IgnoreAspectRatio = false };
            image.Thumbnail(geometry);
            await image.WriteAsync(jpegStream, MagickFormat.Jpeg);
            await _photoRepository.SaveThumbnailAsync(photo, thumbnailSize, jpegStream.ToArray());
        }
    }
}