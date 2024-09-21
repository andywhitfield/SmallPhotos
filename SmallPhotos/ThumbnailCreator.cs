using System.IO;
using System.Threading.Tasks;
using ImageMagick;
using SmallPhotos.Model;

namespace SmallPhotos;

public class ThumbnailCreator : IThumbnailCreator
{
    public async Task<byte[]> CreateThumbnail(Photo photo, MagickImage image, ThumbnailSize thumbnailSize)
    {
        using var thumbnailImage = (MagickImage)image.Clone();
        var resizeTo = thumbnailSize.ToSize();
        thumbnailImage.Thumbnail((uint)resizeTo.Width, (uint)resizeTo.Height);
        thumbnailImage.Extent((uint)resizeTo.Width, (uint)resizeTo.Height, Gravity.Center, MagickColors.Transparent);
        using MemoryStream thumbnailStream = new();
        await thumbnailImage.WriteAsync(thumbnailStream, MagickFormat.Jpeg);
        return thumbnailStream.ToArray();
    }
}