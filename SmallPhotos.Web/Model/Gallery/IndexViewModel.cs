using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model.Gallery
{
    public class IndexViewModel : BaseViewModel
    {
        public IndexViewModel(HttpContext context, PhotoModel photo, PhotoModel? previousPhoto, PhotoModel? nextPhoto) : base(context)
        {
            Photo = photo;
            PreviousPhoto = previousPhoto;
            NextPhoto = nextPhoto;
        }

        public PhotoModel Photo { get; }
        public PhotoModel? PreviousPhoto { get; }
        public PhotoModel? NextPhoto { get; }
    }
}