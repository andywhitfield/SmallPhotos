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

public class PhotoRepository(ILogger<PhotoRepository> logger, SqliteDataContext context)
    : IPhotoRepository
{
    public Task<Photo?> GetAsync(UserAccount user, long photoId) =>
        context
            .Photos!
            .Include(p => p.AlbumSource)
            .FirstOrDefaultAsync(p =>
                p.PhotoId == photoId &&
                p.AlbumSource!.UserAccountId == user.UserAccountId &&
                p.AlbumSource.DeletedDateTime == null &&
                p.DeletedDateTime == null);

    public Task<Photo?> GetAsync(UserAccount user, AlbumSource album, string? filename, string? filepath) =>
        context
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
        context
            .Photos!
            .Include(p => p.AlbumSource)
            .Where(p =>
                p.AlbumSource!.UserAccountId == user.UserAccountId &&
                p.AlbumSource.DeletedDateTime == null &&
                p.DeletedDateTime == null)
            .OrderByDescending(p => p.DateTaken ?? p.FileCreationDateTime)
            .ToListAsync();

    public Task<List<Photo>> GetAllAsync(AlbumSource album) =>
        context
            .Photos!
            .Where(p =>
                p.AlbumSourceId == album.AlbumSourceId &&
                p.DeletedDateTime == null)
            .OrderBy(p => p.FileCreationDateTime)
            .ToListAsync();

    public Task<Thumbnail?> GetThumbnailAsync(Photo photo, ThumbnailSize size) =>
        context
            .Thumbnails!
            .FirstOrDefaultAsync(t => t.PhotoId == photo.PhotoId && t.ThumbnailSize == size);

    public async Task<Photo> AddAsync(AlbumSource album, FileInfo file, Size imageSize, DateTime? dateTaken, string? relativePath = null)
    {
        var photo = context.Photos!.Add(new()
        {
            AlbumSource = album,
            Filename = file.Name,
            RelativePath = relativePath ?? album.Folder.GetRelativePath(file),
            FileCreationDateTime = file.CreationTimeUtc,
            FileModificationDateTime = file.LastWriteTimeUtc,
            DateTaken = dateTaken,
            Width = imageSize.Width,
            Height = imageSize.Height
        });
        await context.SaveChangesAsync();
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

        return context.SaveChangesAsync();
    }

    public async Task<Thumbnail> SaveThumbnailAsync(Photo photo, ThumbnailSize size, byte[] image)
    {
        logger.LogDebug("Saving thumbnail of size {Size} for photo {PhotoId}", size, photo.PhotoId);
        Thumbnail thumbnail = new()
        {
            Photo = photo,
            ThumbnailSize = size,
            ThumbnailImage = image
        };

        context.Thumbnails!.RemoveRange(context.Thumbnails.Where(t => t.PhotoId == photo.PhotoId && t.ThumbnailSize == size));
        context.Thumbnails.Add(thumbnail);
        await context.SaveChangesAsync();
        return thumbnail;
    }

    public Task DeleteAsync(Photo photo)
    {
        photo.DeletedDateTime = DateTime.UtcNow;
        context.Thumbnails!.RemoveRange(context.Thumbnails.Where(t => t.PhotoId == photo.PhotoId));
        return context.SaveChangesAsync();
    }

    public Task<List<Photo>> GetAllStarredAsync(UserAccount user) =>
        context.StarredPhotos!
            .Include(s => s.Photo).ThenInclude(p => p!.AlbumSource)
            .Where(s =>
                s.UserAccountId == user.UserAccountId &&
                s.Photo!.DeletedDateTime == null &&
                s.Photo.AlbumSource!.DeletedDateTime == null)
            .Select(s => s.Photo!)
            .OrderByDescending(p => p.DateTaken ?? p.FileCreationDateTime)
            .ToListAsync();

    public Task<List<Photo>> GetStarredAsync(UserAccount user, ISet<long> photoIds) =>
        context.StarredPhotos!
            .Where(s => s.UserAccountId == user.UserAccountId && photoIds.Contains(s.PhotoId))
            .Select(s => s.Photo!)
            .ToListAsync();

    public async Task StarAsync(UserAccount user, Photo photo)
    {
        if (await context.StarredPhotos!.AnyAsync(s => s.UserAccountId == user.UserAccountId && s.PhotoId == photo.PhotoId))
        {
            logger.LogInformation("Photo {PhotoId} is already starred for user {UserAccountId}, nothing to do", photo.PhotoId, user.UserAccountId);
            return;
        }

        context.StarredPhotos!.Add(new() { UserAccount = user, Photo = photo });
        await context.SaveChangesAsync();
    }

    public Task UnstarAsync(UserAccount user, Photo photo)
    {
        context.StarredPhotos!.RemoveRange(context.StarredPhotos.Where(s => s.UserAccountId == user.UserAccountId && s.PhotoId == photo.PhotoId));
        return context.SaveChangesAsync();
    }

    public Task<List<PhotoTag>> GetTagsAsync(UserAccount user, Photo photo) =>
        context.PhotoTags!
            .Where(t => t.UserAccountId == user.UserAccountId && t.PhotoId == photo.PhotoId)
            .OrderBy(t => t.Tag.ToLower())
            .ToListAsync();

    public async Task<IEnumerable<(string, int)>> GetTagsAndCountAsync(UserAccount user) =>
        (await context.PhotoTags!
            .Where(t => t.UserAccountId == user.UserAccountId)
            .GroupBy(t => t.Tag)
            .Select(t => new { Tag = t.Key, PhotoCount = t.Count() })
            .OrderByDescending (t => t.PhotoCount)
            .ThenBy(t => t.Tag.ToLower())
            .ToListAsync()
        ).Select(t => (t.Tag, t.PhotoCount));

    public Task<List<Photo>> GetAllWithTagAsync(UserAccount user, string tag) =>
        context.PhotoTags!
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
        context.PhotoTags!.Add(new()
        {
            UserAccount = user,
            Photo = photo,
            Tag = tag.Trim(),
            CreatedDateTime = DateTime.UtcNow
        });
        return context.SaveChangesAsync();
    }

    public Task DeleteTagsAsync(UserAccount user, Photo photo)
    {
        context.PhotoTags!.RemoveRange(context.PhotoTags!.Where(t => t.UserAccountId == user.UserAccountId && t.PhotoId == photo.PhotoId));
        return context.SaveChangesAsync();
    }
}