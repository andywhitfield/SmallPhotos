using System;
using System.Drawing;

namespace SmallPhotos.Web.Model
{
    public class PhotoModel
    {
        public PhotoModel(long photoId, string filename, Size size, DateTime dateTaken)
        {
            PhotoId = photoId;
            Filename = filename;
            Size = size;
            DateTaken = dateTaken.ToString("HH:mm:ss' on 'dd MMMM yyyy");
        }

        public long PhotoId { get; }
        public string Filename { get; }
        public Size Size { get; }
        public string DateTaken { get; }
    }
}