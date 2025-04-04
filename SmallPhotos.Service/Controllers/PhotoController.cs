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
public class PhotoController(
    IUserAccountRepository userAccountRepository,
    IAlbumRepository albumRepository,
    IPhotoRepository photoRepository,
    IThumbnailCreator thumbnailCreator)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Photo>> Post(CreateOrUpdatePhotoRequest request)
    {
        var userAccount = await userAccountRepository.GetAsync(request.UserAccountId);
        if (userAccount == null)
            return BadRequest($"User [{request.UserAccountId}] not found");

        var albumSource = await albumRepository.GetAsync(userAccount, request.AlbumSourceId);
        if (albumSource == null)
            return BadRequest($"Album [{request.AlbumSourceId}] not found");

        FileInfo file = new(albumSource.IsDropboxSource ? (request.Filename ?? "") : albumSource.PhotoPath(request.FilePath, request.Filename ?? ""));
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

        using MagickImage image = new(file.FullName, format);
        Size originalSize = new((int)image.Width, (int)image.Height);

        var photo = await photoRepository.GetAsync(userAccount, albumSource, file.Name, request.FilePath);
        if (photo == null)
            photo = await photoRepository.AddAsync(albumSource, file, originalSize, ExtractDateTaken(image), albumSource.IsDropboxSource ? request.FilePath : null);
        else
            await photoRepository.UpdateAsync(photo, file, originalSize, ExtractDateTaken(image));

        foreach (var thumbnailSize in Enum.GetValues<ThumbnailSize>())
        {
            var thumbnail = await thumbnailCreator.CreateThumbnail(photo, image, thumbnailSize);
            await photoRepository.SaveThumbnailAsync(photo, thumbnailSize, thumbnail);
        }

        return photo;
    }

    private static DateTime? ExtractDateTaken(MagickImage image)
    {
        var exifData = image.GetExifProfile();

        return ExtractDateTaken(ExifTag.DateTimeOriginal)
            ?? ExtractDateTaken(ExifTag.DateTimeDigitized)
            ?? ExtractDateTaken(ExifTag.DateTime);

        DateTime? ExtractDateTaken(ExifTag<string> tag) =>
            DateTime.TryParseExact(exifData?.GetValue<string>(tag)?.Value, "yyyy:MM:dd HH:mm:ss", null, DateTimeStyles.AssumeLocal, out var timeTaken) ? timeTaken : default(DateTime?);
    }
}