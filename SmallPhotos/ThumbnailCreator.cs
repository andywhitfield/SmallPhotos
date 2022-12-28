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
        thumbnailImage.Thumbnail(resizeTo.Width, resizeTo.Height);
        thumbnailImage.Extent(resizeTo.Width, resizeTo.Height, Gravity.Center, MagickColors.Transparent);
        using var thumbnailStream = new MemoryStream();
        await thumbnailImage.WriteAsync(thumbnailStream, MagickFormat.Jpeg);
        return thumbnailStream.ToArray();
    }
}