using System.IO;

namespace SmallPhotos.Web.Handlers.Models
{
    public class GetPhotoResponse
    {
        public static readonly GetPhotoResponse Empty = new GetPhotoResponse(null, null);
        
        public GetPhotoResponse(Stream imageStream, string imageContentType)
        {
            ImageStream = imageStream;
            ImageContentType = imageContentType;
        }

        public Stream ImageStream { get; }
        public string ImageContentType { get; }
    }
}