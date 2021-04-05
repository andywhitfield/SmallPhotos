using System.Collections.Generic;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers.Models
{
    public class HomePageResponse
    {
        public HomePageResponse(IEnumerable<PhotoModel> photos) => Photos = photos;
        public IEnumerable<PhotoModel> Photos { get; }
    }
}