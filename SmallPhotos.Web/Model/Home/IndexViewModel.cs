using System.Collections.Generic;
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
        }

        public ThumbnailSize ThumbnailSize { get; }
        public IEnumerable<PhotoModel> Photos { get; }
        public Pagination Pagination { get; }
    }
}