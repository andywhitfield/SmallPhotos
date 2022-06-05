using System.IO;
using System.Threading.Tasks;

namespace SmallPhotos
{
    public interface IPhotoReader
    {
        Task<(string? ContentType, Stream? ContentStream)> GetPhotoStreamForWebAsync(FileInfo file);
    }
}