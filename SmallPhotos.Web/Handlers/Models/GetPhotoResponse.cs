using System.IO;

namespace SmallPhotos.Web.Handlers.Models
{
    public class GetPhotoResponse
    {
        public static readonly GetPhotoResponse Empty = new GetPhotoResponse(null);
        
        public GetPhotoResponse(Stream imageStream) => ImageStream = imageStream;

        public Stream ImageStream { get; }
    }
}