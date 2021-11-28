using System.Collections.Generic;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers.Models
{
    public class HomePageResponse
    {
        public HomePageResponse(bool isUserValid, IEnumerable<PhotoModel> photos)
        {
            IsUserValid = isUserValid;
            Photos = photos;
        }
        
        public bool IsUserValid { get; }
        public IEnumerable<PhotoModel> Photos { get; }
    }
}