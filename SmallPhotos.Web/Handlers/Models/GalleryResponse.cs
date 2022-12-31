using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers.Models;

public class GalleryResponse
{
    public GalleryResponse(PhotoModel? photo, PhotoModel? previousPhoto, PhotoModel? nextPhoto, int photoNumber, int photoCount)
    {
        Photo = photo;
        PreviousPhoto = previousPhoto;
        NextPhoto = nextPhoto;
        PhotoNumber = photoNumber;
        PhotoCount = photoCount;
    }

    public PhotoModel? Photo { get; }
    public PhotoModel? PreviousPhoto { get; }
    public PhotoModel? NextPhoto { get; }
    public int PhotoNumber { get; }
    public int PhotoCount { get; }
}