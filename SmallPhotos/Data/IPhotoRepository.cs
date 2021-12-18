using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SmallPhotos.Model;

namespace SmallPhotos.Data
{
    public interface IPhotoRepository
    {
        Task<Photo> GetAsync(UserAccount user, long photoId);
        Task<List<Photo>> GetAllAsync(UserAccount user);
        Task<List<Photo>> GetAllAsync(AlbumSource album);
        Task<Thumbnail> GetThumbnailAsync(Photo photo, ThumbnailSize size);
        Task<Photo> AddAsync(AlbumSource album, FileInfo file);
        Task UpdateAsync(Photo photo, FileInfo file);
        Task<Thumbnail> SaveThumbnailAsync(Photo photo, ThumbnailSize size, byte[] image);
        Task DeleteAsync(Photo photo);
    }
}