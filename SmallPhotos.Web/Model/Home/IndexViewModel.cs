using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model.Home
{
    public class IndexViewModel : BaseViewModel
    {
        public IndexViewModel(HttpContext context) : base(context) { }
    }
}