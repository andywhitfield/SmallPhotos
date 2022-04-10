using System.Collections.Generic;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers.Models
{
    public class HomePageResponse
    {
        public HomePageResponse(bool isUserValid, IEnumerable<PhotoModel> photos, Pagination pagination)
        {
            IsUserValid = isUserValid;
            Photos = photos;
            Pagination = pagination;
        }
        
        public bool IsUserValid { get; }
        public IEnumerable<PhotoModel> Photos { get; }
        public Pagination Pagination { get; }
    }
}