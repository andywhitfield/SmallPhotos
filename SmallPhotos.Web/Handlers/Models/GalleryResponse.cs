using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers.Models
{
    public class GalleryResponse
    {
        public GalleryResponse(PhotoModel photo) => Photo = photo;

        public PhotoModel Photo { get; }
    }
}