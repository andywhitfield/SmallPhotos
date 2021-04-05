namespace SmallPhotos.Web.Model
{
    public class PhotoModel
    {
        public PhotoModel(string filename) => Filename = filename;
        public string Filename { get; }
    }
}