using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using SmallPhotos.Model;

namespace SmallPhotos.Data;

public interface IPhotoRepository
{
    Task<Photo?> GetAsync(UserAccount user, long photoId);
    Task<Photo?> GetAsync(UserAccount user, AlbumSource album, string? filename, string? filepath);
    Task<List<Photo>> GetAllAsync(UserAccount user);
    Task<List<Photo>> GetAllAsync(AlbumSource album);
    Task<Thumbnail?> GetThumbnailAsync(Photo photo, ThumbnailSize size);
    Task<Photo> AddAsync(AlbumSource album, FileInfo file, Size imageSize, DateTime? dateTaken, string? relativePath = null);
    Task UpdateAsync(Photo photo, FileInfo file, Size imageSize, DateTime? dateTaken);
    Task<Thumbnail> SaveThumbnailAsync(Photo photo, ThumbnailSize size, byte[] image);
    Task DeleteAsync(Photo photo);
    Task<List<Photo>> GetAllStarredAsync(UserAccount user);
    Task<List<Photo>> GetStarredAsync(UserAccount user, ISet<long> photoIds);
    Task StarAsync(UserAccount user, Photo photo);
    Task UnstarAsync(UserAccount user, Photo photo);
    Task<List<PhotoTag>> GetTagsAsync(UserAccount user, Photo photo);
    Task<IEnumerable<(string, int)>> GetTagsAndCountAsync(UserAccount user);
    Task<List<Photo>> GetAllWithTagAsync(UserAccount user, string tag);
    Task AddTagAsync(UserAccount user, Photo photo, string tag);
    Task DeleteTagsAsync(UserAccount user, Photo photo);
}