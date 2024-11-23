using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model.Tagged;

public class IndexViewModel(HttpContext context, IEnumerable<(string Tag, int PhotoCount)> photoTagsAndCount)
    : BaseViewModel(context, SelectedView.Tagged)
{
    public IEnumerable<PhotoTagModel> PhotoTags { get; } = photoTagsAndCount.Select(t => new PhotoTagModel(t.Tag, t.PhotoCount));
}