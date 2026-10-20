using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers.Models;

public class MapPageResponse(bool isUserValid, IEnumerable<PhotoModel> photos)
{
    public bool IsUserValid { get; } = isUserValid;
    public IEnumerable<PhotoModel> Photos { get; } = photos;
}