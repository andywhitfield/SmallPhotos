using System.Threading.Tasks;
using ImageMagick;
using SmallPhotos.Model;

namespace SmallPhotos
{
    public interface IThumbnailCreator
    {
        Task<byte[]> CreateThumbnail(Photo photo, MagickImage image, ThumbnailSize thumbnailSize);
    }
}