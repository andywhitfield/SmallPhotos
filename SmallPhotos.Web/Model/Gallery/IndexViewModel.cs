using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model.Gallery
{
    public class IndexViewModel : BaseViewModel
    {
        public IndexViewModel(HttpContext context, PhotoModel photo) : base(context) => Photo = photo;

        public PhotoModel Photo { get; }
    }
}