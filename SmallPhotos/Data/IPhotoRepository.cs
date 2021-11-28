using System.Collections.Generic;
using System.Threading.Tasks;
using SmallPhotos.Model;

namespace SmallPhotos.Data
{
    public interface IPhotoRepository
    {
        Task<Photo> GetAsync(UserAccount user, long photoId);
        Task<List<Photo>> GetAllAsync(UserAccount user);
        Task<Thumbnail> GetThumbnailAsync(Photo photo, ThumbnailSize size);
        Task<Thumbnail> SaveThumbnailAsync(Photo photo, ThumbnailSize size, byte[] image);
    }
}