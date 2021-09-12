using System.IO;

namespace SmallPhotos.Web.Handlers.Models
{
    public class GetPhotoResponse
    {
        public GetPhotoResponse(Stream imageStream) => ImageStream = imageStream;

        public Stream ImageStream { get; }
    }
}