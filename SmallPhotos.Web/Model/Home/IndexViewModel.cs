using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using SmallPhotos.Model;

namespace SmallPhotos.Web.Model.Home
{
    public class IndexViewModel : BaseViewModel
    {
        public IndexViewModel(HttpContext context, ThumbnailSize thumbnailSize, IEnumerable<PhotoModel> photos)
            : base(context)
        {
            ThumbnailSize = thumbnailSize;
            Photos = photos;
        }

        public ThumbnailSize ThumbnailSize { get; }
        public IEnumerable<PhotoModel> Photos { get; }
    }
}