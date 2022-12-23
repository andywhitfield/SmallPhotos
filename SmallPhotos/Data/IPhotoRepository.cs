using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using SmallPhotos.Model;

namespace SmallPhotos.Data
{
    public interface IPhotoRepository
    {
        Task<Photo?> GetAsync(UserAccount user, long photoId);
        Task<Photo?> GetAsync(UserAccount user, AlbumSource album, string? filename, string? filepath);
        Task<List<Photo>> GetAllAsync(UserAccount user);
        Task<List<Photo>> GetAllAsync(AlbumSource album);
        Task<Thumbnail?> GetThumbnailAsync(Photo photo, ThumbnailSize size);
        Task<Photo> AddAsync(AlbumSource album, FileInfo file, Size imageSize, DateTime? dateTaken);
        Task UpdateAsync(Photo photo, FileInfo file, Size imageSize, DateTime? dateTaken);
        Task<Thumbnail> SaveThumbnailAsync(Photo photo, ThumbnailSize size, byte[] image);
        Task DeleteAsync(Photo photo);
    }
}