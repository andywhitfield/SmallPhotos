using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.AspNetCore.Mvc;
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Service.Models;

namespace SmallPhotos.Service.Controllers;

[ApiController, Route("api/[controller]")]
public class PhotoController : ControllerBase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAlbumRepository _albumRepository;
    private readonly IPhotoRepository _photoRepository;
    private readonly IThumbnailCreator _thumbnailCreator;

    public PhotoController(
        IUserAccountRepository userAccountRepository,
        IAlbumRepository albumRepository,
        IPhotoRepository photoRepository,
        IThumbnailCreator thumbnailCreator)
    {
        _userAccountRepository = userAccountRepository;
        _albumRepository = albumRepository;
        _photoRepository = photoRepository;
        _thumbnailCreator = thumbnailCreator;
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

        var file = new FileInfo(albumSource.PhotoPath(request.FilePath, request.Filename ?? ""));
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

        using var image = new MagickImage(file.FullName, format);
        var originalSize = new Size(image.Width, image.Height);

        var photo = await _photoRepository.GetAsync(userAccount, albumSource, request.Filename, request.FilePath);
        if (photo == null)
            photo = await _photoRepository.AddAsync(albumSource, file, originalSize, ExtractDateTaken(image));
        else
            await _photoRepository.UpdateAsync(photo, file, originalSize, ExtractDateTaken(image));

        foreach (var thumbnailSize in Enum.GetValues<ThumbnailSize>())
        {
            var thumbnail = await _thumbnailCreator.CreateThumbnail(photo, image, thumbnailSize);
            await _photoRepository.SaveThumbnailAsync(photo, thumbnailSize, thumbnail);
        }

        return photo;
    }

    private DateTime? ExtractDateTaken(MagickImage image)
    {
        var exifData = image.GetExifProfile();
        
        return ExtractDateTaken(ExifTag.DateTimeOriginal)
            ?? ExtractDateTaken(ExifTag.DateTimeDigitized)
            ?? ExtractDateTaken(ExifTag.DateTime);

        DateTime? ExtractDateTaken(ExifTag<string> tag) =>
            DateTime.TryParseExact(exifData?.GetValue<string>(tag)?.Value, "yyyy:MM:dd HH:mm:ss", null, DateTimeStyles.AssumeLocal, out var timeTaken) ? timeTaken : default(DateTime?);
    }
}