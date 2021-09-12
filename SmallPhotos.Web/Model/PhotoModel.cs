using System.Drawing;

namespace SmallPhotos.Web.Model
{
    public class PhotoModel
    {
        public PhotoModel(long photoId, string filename, Size size)
        {
            PhotoId = photoId;
            Filename = filename;
            Size = size;
        }

        public long PhotoId { get; }
        public string Filename { get; }
        public Size Size { get; }
    }
}