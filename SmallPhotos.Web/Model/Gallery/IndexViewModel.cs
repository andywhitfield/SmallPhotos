using Microsoft.AspNetCore.Http;

namespace SmallPhotos.Web.Model.Gallery;

public class IndexViewModel(HttpContext context, PhotoModel photo, PhotoModel? previousPhoto,
    PhotoModel? nextPhoto, int photoNumber, int photoCount, string? fromPage)
    : BaseViewModel(context, fromPage == "starred" ? SelectedView.Starred : (fromPage?.StartsWith("tagged_") ?? false) ? SelectedView.Tagged : SelectedView.All)
{
    public PhotoModel Photo { get; } = photo;
    public PhotoModel? PreviousPhoto { get; } = previousPhoto;
    public PhotoModel? NextPhoto { get; } = nextPhoto;
    public int PreviousPhotoNumber { get; } = previousPhoto == null ? 1 : photoNumber - 1;
    public int NextPhotoNumber { get; } = nextPhoto == null ? 1 : photoNumber + 1;
    public int PhotoCount { get; } = photoCount;
    public string? FromPage { get; } = fromPage;

    public string FromPagePath => FromPage switch {
        "starred" => "/starred",
        {} when FromPage.StartsWith("tagged_") => $"/tagged/{FromPage.Substring("tagged_".Length)}",
        _ => "/"
    };

    public string FromPageQueryString => FromPage == "starred" || (FromPage?.StartsWith("tagged_") ?? false) ? "?from=" + FromPage : "";
}