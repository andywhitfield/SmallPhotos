using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model.Gallery;

public class IndexViewModel : BaseViewModel
{
    public IndexViewModel(HttpContext context, PhotoModel photo, PhotoModel? previousPhoto, PhotoModel? nextPhoto, int photoNumber, int photoCount, string? fromPage) : base(context, fromPage == "starred" ? SelectedView.Starred : SelectedView.All)
    {
        Photo = photo;
        PreviousPhoto = previousPhoto;
        NextPhoto = nextPhoto;
        PreviousPhotoNumber = previousPhoto == null ? 1 : photoNumber - 1;
        NextPhotoNumber = nextPhoto == null ? 1 : photoNumber + 1;
        PhotoCount = photoCount;
        FromPage = fromPage;
    }

    public PhotoModel Photo { get; }
    public PhotoModel? PreviousPhoto { get; }
    public PhotoModel? NextPhoto { get; }
    public int PreviousPhotoNumber { get; }
    public int NextPhotoNumber { get; }
    public int PhotoCount { get; }
    public string? FromPage { get; }

    public string FromPagePath => FromPage switch {
        "starred" => "/starred",
        {} when FromPage.StartsWith("tagged_") => $"/tagged/{FromPage.Substring("tagged_".Length)}",
        _ => "/"
    };

    public string FromPageQueryString => FromPage == "starred" || (FromPage?.StartsWith("tagged_") ?? false) ? "?from=" + FromPage : "";
}