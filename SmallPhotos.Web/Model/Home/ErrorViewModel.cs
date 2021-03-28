using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model.Home
{
    public class ErrorViewModel : BaseViewModel
    {
        public ErrorViewModel(HttpContext context) : base(context) { }
    }
}