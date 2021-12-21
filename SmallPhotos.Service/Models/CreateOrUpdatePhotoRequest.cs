namespace SmallPhotos.Service.Models
{
    public class CreateOrUpdatePhotoRequest
    {
        public long UserAccountId { get; set; }
        public long AlbumSourceId { get; set; }
        public string Filename { get; set; }
    }
}