using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallPhotos.Model;

namespace SmallPhotos.Data
{
    public class PhotoRepository : IPhotoRepository
    {
        private readonly ILogger<PhotoRepository> _logger;
        private readonly SqliteDataContext _context;

        public PhotoRepository(ILogger<PhotoRepository> logger, SqliteDataContext context)
        {
            _logger = logger;
            _context = context;
        }

        public Task<Photo> GetAsync(UserAccount user, long photoId) =>
            _context
                .Photos
                .Include(p => p.AlbumSource)
                .FirstOrDefaultAsync(p =>
                    p.PhotoId == photoId &&
                    p.AlbumSource.UserAccountId == user.UserAccountId &&
                    p.AlbumSource.DeletedDateTime == null &&
                    p.DeletedDateTime == null);

        public Task<List<Photo>> GetAllAsync(UserAccount user) =>
            _context
                .Photos
                .Where(p =>
                    p.AlbumSource.UserAccountId == user.UserAccountId &&
                    p.AlbumSource.DeletedDateTime == null &&
                    p.DeletedDateTime == null)
                .OrderBy(p => p.FileCreationDateTime)
                .ToListAsync();

        public Task<List<Photo>> GetAllAsync(AlbumSource album) =>
            _context
                .Photos
                .Where(p =>
                    p.AlbumSourceId == album.AlbumSourceId &&
                    p.DeletedDateTime == null)
                .OrderBy(p => p.FileCreationDateTime)
                .ToListAsync();

        public Task<Thumbnail> GetThumbnailAsync(Photo photo, ThumbnailSize size) =>
            _context
                .Thumbnails
                .FirstOrDefaultAsync(t => t.PhotoId == photo.PhotoId && t.ThumbnailSize == size);

        public async Task<Photo> AddAsync(AlbumSource album, FileInfo file)
        {
            var photo = await _context.Photos.AddAsync(new Photo
            {
                AlbumSource = album,
                Filename = file.Name,
                FileCreationDateTime = file.CreationTimeUtc,
                FileModificationDateTime = file.LastWriteTimeUtc,
                Width = 100, // TODO - read image, create thumbnails, set width & height
                Height = 100
            });
            await _context.SaveChangesAsync();
            return photo.Entity;
        }

        public Task UpdateAsync(Photo photo, FileInfo file)
        {
            photo.FileCreationDateTime = file.CreationTimeUtc;
            photo.FileModificationDateTime = file.LastWriteTimeUtc;
            // TODO - read image, create thumbnails, set width & height
            photo.Width = 100;
            photo.Height = 100;

            return _context.SaveChangesAsync();
        }

        public async Task<Thumbnail> SaveThumbnailAsync(Photo photo, ThumbnailSize size, byte[] image)
        {
            _logger.LogDebug($"Saving thumbnail of size {size} for photo {photo.PhotoId}");
            var thumbnail = new Thumbnail
            {
                Photo = photo,
                ThumbnailSize = size,
                ThumbnailImage = image
            };

            _context.Thumbnails.RemoveRange(_context.Thumbnails.Where(t => t.PhotoId == photo.PhotoId && t.ThumbnailSize == size));
            await _context.Thumbnails.AddAsync(thumbnail);
            await _context.SaveChangesAsync();
            return thumbnail;
        }

        public Task DeleteAsync(Photo photo)
        {
            photo.DeletedDateTime = DateTime.UtcNow;
            _context.Thumbnails.RemoveRange(_context.Thumbnails.Where(t => t.PhotoId == photo.PhotoId));
            return _context.SaveChangesAsync();
        }
    }
}