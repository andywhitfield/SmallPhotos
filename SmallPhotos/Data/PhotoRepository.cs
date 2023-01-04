using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallPhotos.Model;

namespace SmallPhotos.Data;

public class PhotoRepository : IPhotoRepository
{
    private readonly ILogger<PhotoRepository> _logger;
    private readonly SqliteDataContext _context;

    public PhotoRepository(ILogger<PhotoRepository> logger, SqliteDataContext context)
    {
        _logger = logger;
        _context = context;
    }

    public Task<Photo?> GetAsync(UserAccount user, long photoId) =>
        _context
            .Photos!
            .Include(p => p.AlbumSource)
            .FirstOrDefaultAsync(p =>
                p.PhotoId == photoId &&
                p.AlbumSource!.UserAccountId == user.UserAccountId &&
                p.AlbumSource.DeletedDateTime == null &&
                p.DeletedDateTime == null);

    public Task<Photo?> GetAsync(UserAccount user, AlbumSource album, string? filename, string? filepath) =>
        _context
            .Photos!
            .Include(p => p.AlbumSource)
            .FirstOrDefaultAsync(p =>
                p.Filename == filename &&
                (string.IsNullOrEmpty(p.RelativePath) ? "" : p.RelativePath) == (string.IsNullOrEmpty(filepath) ? "" : filepath) &&
                p.AlbumSource!.AlbumSourceId == album.AlbumSourceId &&
                p.AlbumSource.UserAccountId == user.UserAccountId &&
                p.AlbumSource.DeletedDateTime == null &&
                p.DeletedDateTime == null);

    public Task<List<Photo>> GetAllAsync(UserAccount user) =>
        _context
            .Photos!
            .Include(p => p.AlbumSource)
            .Where(p =>
                p.AlbumSource!.UserAccountId == user.UserAccountId &&
                p.AlbumSource.DeletedDateTime == null &&
                p.DeletedDateTime == null)
            .OrderByDescending(p => p.DateTaken ?? p.FileCreationDateTime)
            .ToListAsync();

    public Task<List<Photo>> GetAllAsync(AlbumSource album) =>
        _context
            .Photos!
            .Where(p =>
                p.AlbumSourceId == album.AlbumSourceId &&
                p.DeletedDateTime == null)
            .OrderBy(p => p.FileCreationDateTime)
            .ToListAsync();

    public Task<Thumbnail?> GetThumbnailAsync(Photo photo, ThumbnailSize size) =>
        _context
            .Thumbnails!
            .FirstOrDefaultAsync(t => t.PhotoId == photo.PhotoId && t.ThumbnailSize == size);

    public async Task<Photo> AddAsync(AlbumSource album, FileInfo file, Size imageSize, DateTime? dateTaken)
    {
        var photo = _context.Photos!.Add(new()
        {
            AlbumSource = album,
            Filename = file.Name,
            RelativePath = album.Folder.GetRelativePath(file),
            FileCreationDateTime = file.CreationTimeUtc,
            FileModificationDateTime = file.LastWriteTimeUtc,
            DateTaken = dateTaken,
            Width = imageSize.Width,
            Height = imageSize.Height
        });
        await _context.SaveChangesAsync();
        return photo.Entity;
    }

    public Task UpdateAsync(Photo photo, FileInfo file, Size imageSize, DateTime? dateTaken)
    {
        photo.FileCreationDateTime = file.CreationTimeUtc;
        photo.FileModificationDateTime = file.LastWriteTimeUtc;
        photo.DateTaken = dateTaken;
        photo.Width = imageSize.Width;
        photo.Height = imageSize.Height;
        photo.LastUpdateDateTime = DateTime.UtcNow;

        return _context.SaveChangesAsync();
    }

    public async Task<Thumbnail> SaveThumbnailAsync(Photo photo, ThumbnailSize size, byte[] image)
    {
        _logger.LogDebug($"Saving thumbnail of size {size} for photo {photo.PhotoId}");
        Thumbnail thumbnail = new()
        {
            Photo = photo,
            ThumbnailSize = size,
            ThumbnailImage = image
        };

        _context.Thumbnails!.RemoveRange(_context.Thumbnails.Where(t => t.PhotoId == photo.PhotoId && t.ThumbnailSize == size));
        _context.Thumbnails.Add(thumbnail);
        await _context.SaveChangesAsync();
        return thumbnail;
    }

    public Task DeleteAsync(Photo photo)
    {
        photo.DeletedDateTime = DateTime.UtcNow;
        _context.Thumbnails!.RemoveRange(_context.Thumbnails.Where(t => t.PhotoId == photo.PhotoId));
        return _context.SaveChangesAsync();
    }

    public Task<List<Photo>> GetAllStarredAsync(UserAccount user) =>
        _context.StarredPhotos!
            .Include(s => s.Photo).ThenInclude(p => p!.AlbumSource)
            .Where(s =>
                s.UserAccountId == user.UserAccountId &&
                s.Photo!.DeletedDateTime == null &&
                s.Photo.AlbumSource!.DeletedDateTime == null)
            .Select(s => s.Photo!)
            .OrderByDescending(p => p.DateTaken ?? p.FileCreationDateTime)
            .ToListAsync();

    public Task<List<Photo>> GetStarredAsync(UserAccount user, ISet<long> photoIds) =>
        _context.StarredPhotos!
            .Where(s => s.UserAccountId == user.UserAccountId && photoIds.Contains(s.PhotoId))
            .Select(s => s.Photo!)
            .ToListAsync();

    public async Task StarAsync(UserAccount user, Photo photo)
    {
        if (await _context.StarredPhotos!.AnyAsync(s => s.UserAccountId == user.UserAccountId && s.PhotoId == photo.PhotoId))
        {
            _logger.LogInformation($"Photo {photo.PhotoId} is already starred for user {user.UserAccountId}, nothing to do");
            return;
        }

        _context.StarredPhotos!.Add(new() { UserAccount = user, Photo = photo });
        await _context.SaveChangesAsync();
    }

    public Task UnstarAsync(UserAccount user, Photo photo)
    {
        _context.StarredPhotos!.RemoveRange(_context.StarredPhotos.Where(s => s.UserAccountId == user.UserAccountId && s.PhotoId == photo.PhotoId));
        return _context.SaveChangesAsync();
    }

    public Task<List<PhotoTag>> GetTagsAsync(UserAccount user, Photo photo) =>
        _context.PhotoTags!
            .Where(t => t.UserAccountId == user.UserAccountId && t.PhotoId == photo.PhotoId)
            .OrderBy(t => t.Tag.ToLower())
            .ToListAsync();

    public async Task<IEnumerable<(string, int)>> GetTagsAndCountAsync(UserAccount user) =>
        (await _context.PhotoTags!
            .Where(t => t.UserAccountId == user.UserAccountId)
            .GroupBy(t => t.Tag)
            .Select(t => new { Tag = t.Key, PhotoCount = t.Count() })
            .OrderByDescending (t => t.PhotoCount)
            .ThenBy(t => t.Tag.ToLower())
            .ToListAsync()
        ).Select(t => (t.Tag, t.PhotoCount));

    public Task<List<Photo>> GetAllWithTagAsync(UserAccount user, string tag) =>
        _context.PhotoTags!
            .Include(t => t.Photo).ThenInclude(p => p!.AlbumSource)
            .Where(t =>
                t.UserAccountId == user.UserAccountId &&
                t.Tag == tag &&
                t.Photo!.DeletedDateTime == null &&
                t.Photo.AlbumSource!.DeletedDateTime == null)
            .Select(t => t.Photo!)
            .OrderByDescending(p => p.DateTaken ?? p.FileCreationDateTime)
            .ToListAsync();

    public Task AddTagAsync(UserAccount user, Photo photo, string tag)
    {
        _context.PhotoTags!.Add(new()
        {
            UserAccount = user,
            Photo = photo,
            Tag = tag.Trim(),
            CreatedDateTime = DateTime.UtcNow
        });
        return _context.SaveChangesAsync();
    }

    public Task DeleteTagsAsync(UserAccount user, Photo photo)
    {
        _context.PhotoTags!.RemoveRange(_context.PhotoTags!.Where(t => t.UserAccountId == user.UserAccountId && t.PhotoId == photo.PhotoId));
        return _context.SaveChangesAsync();
    }
}