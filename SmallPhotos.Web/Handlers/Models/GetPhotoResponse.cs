using System.IO;

namespace SmallPhotos.Web.Handlers.Models
{
    public class GetPhotoResponse
    {
        public GetPhotoResponse(FileInfo file) => File = file;

        public FileInfo File { get; }
    }
}