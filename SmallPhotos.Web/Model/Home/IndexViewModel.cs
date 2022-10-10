using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using SmallPhotos.Model;

namespace SmallPhotos.Web.Model.Home
{
    public class IndexViewModel : BaseViewModel
    {
        public IndexViewModel(HttpContext context, ThumbnailSize thumbnailSize, IEnumerable<PhotoModel> photos, Pagination pagination)
            : base(context)
        {
            ThumbnailSize = thumbnailSize;
            Photos = photos;
            Pagination = pagination;

            if (Photos.Any())
            {
                var firstPhotoByDate = Photos.OrderBy(p => p.DateTimeTaken).First();
                var lastPhotoByDate = Photos.OrderByDescending(p => p.DateTimeTaken).First();
                if (firstPhotoByDate.DateTaken == lastPhotoByDate.DateTaken)
                    ImageDateRange = firstPhotoByDate.DateTaken;
                else if (firstPhotoByDate.DateTimeTaken.Date == lastPhotoByDate.DateTimeTaken.Date)
                    ImageDateRange = $"{firstPhotoByDate.DateTaken} - {lastPhotoByDate.DateTaken}";
                else
                    ImageDateRange = $"{firstPhotoByDate.DateTimeTaken:dd MMM yyyy} - {lastPhotoByDate.DateTimeTaken:dd MMM yyyy}";
            }
            else
            {
                ImageDateRange = "";
            }
        }

        public ThumbnailSize ThumbnailSize { get; }
        public IEnumerable<PhotoModel> Photos { get; }
        public Pagination Pagination { get; }
        public string ImageDateRange { get; }
    }
}