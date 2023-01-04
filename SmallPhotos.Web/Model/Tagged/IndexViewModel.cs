using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model.Tagged;

public class IndexViewModel : BaseViewModel
{
    public IndexViewModel(HttpContext context, IEnumerable<(string Tag, int PhotoCount)> photoTagsAndCount)
        : base(context, SelectedView.Tagged)
        => PhotoTags = photoTagsAndCount.Select(t => new PhotoTagModel(t.Tag, t.PhotoCount));

    public IEnumerable<PhotoTagModel> PhotoTags { get; }
}