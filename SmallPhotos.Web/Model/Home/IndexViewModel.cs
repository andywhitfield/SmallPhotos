using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model.Home
{
    public class IndexViewModel : BaseViewModel
    {
        public IndexViewModel(HttpContext context, IEnumerable<PhotoModel> photos) : base(context) =>
            Photos = photos;

        public IEnumerable<PhotoModel> Photos { get; }
    }
}