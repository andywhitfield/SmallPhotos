using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model.Profile
{
    public class IndexViewModel : BaseViewModel
    {
        public IndexViewModel(HttpContext context) : base(context) {}
    }
}