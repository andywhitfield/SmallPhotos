using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers.Models;

public class GalleryResponse(PhotoModel? photo, PhotoModel? previousPhoto, PhotoModel? nextPhoto,
    int photoNumber, int photoCount)
{
    public PhotoModel? Photo { get; } = photo;
    public PhotoModel? PreviousPhoto { get; } = previousPhoto;
    public PhotoModel? NextPhoto { get; } = nextPhoto;
    public int PhotoNumber { get; } = photoNumber;
    public int PhotoCount { get; } = photoCount;
}