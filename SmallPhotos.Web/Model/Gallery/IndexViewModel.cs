using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model.Gallery
{
    public class IndexViewModel : BaseViewModel
    {
        public IndexViewModel(HttpContext context, PhotoModel photo, PhotoModel? previousPhoto, PhotoModel? nextPhoto, int photoNumber, int photoCount) : base(context)
        {
            Photo = photo;
            PreviousPhoto = previousPhoto;
            NextPhoto = nextPhoto;
            PreviousPhotoNumber = previousPhoto == null ? 1 : photoNumber - 1;
            NextPhotoNumber = nextPhoto == null ? 1 : photoNumber + 1;
            PhotoCount = photoCount;
        }

        public PhotoModel Photo { get; }
        public PhotoModel? PreviousPhoto { get; }
        public PhotoModel? NextPhoto { get; }
        public int PreviousPhotoNumber { get; }
        public int NextPhotoNumber { get; }
        public int PhotoCount { get; }
    }
}