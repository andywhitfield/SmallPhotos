using System;
using System.Drawing;

namespace SmallPhotos.Web.Model
{
    public class PhotoModel
    {
        private const string _dateFormat = "HH:mm' on 'dd MMMM yyyy";

        public PhotoModel(long photoId, string source, string filename, string filepath, Size size, DateTime dateTaken, DateTime fileCreationDate)
        {
            PhotoId = photoId;
            Source = source;
            Filename = filename;
            Filepath = filepath;
            Size = size;
            SizeInfo = $"{size.Width}w x {size.Height}h";
            DateTimeTaken = dateTaken;
            DateTaken = dateTaken.ToString(_dateFormat);
            FileCreationDate = fileCreationDate.ToString(_dateFormat);            
        }

        public long PhotoId { get; }
        public string Source { get; }
        public string Filename { get; }
        public string Filepath { get; }
        public Size Size { get; }
        public string SizeInfo { get; }
        public DateTime DateTimeTaken { get; }
        public string DateTaken { get; }
        public string FileCreationDate { get; }
    }
}