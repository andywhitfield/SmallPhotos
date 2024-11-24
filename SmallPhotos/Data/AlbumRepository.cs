using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmallPhotos.Model;

namespace SmallPhotos.Data;

public class AlbumRepository(SqliteDataContext context)
    : IAlbumRepository
{
    public Task<AlbumSource?> GetAsync(UserAccount user, long albumSourceId) =>
        context
            .AlbumSources!
            .FirstOrDefaultAsync(a =>
                a.UserAccountId == user.UserAccountId &&
                a.AlbumSourceId == albumSourceId &&
                a.DeletedDateTime == null);

    public Task<List<AlbumSource>> GetAllAsync(UserAccount user) =>
        context
            .AlbumSources!
            .Where(a => a.UserAccountId == user.UserAccountId && a.DeletedDateTime == null)
            .ToListAsync();

    public Task AddAsync(UserAccount userAccount, string folder, bool recursive, string? dropboxAccessToken = null, string? dropboxRefreshToken = null)
    {
        context.AlbumSources!.Add(new()
        {
            UserAccount = userAccount,
            Folder = folder,
            RecurseSubFolders = recursive,
            DropboxAccessToken = dropboxAccessToken,
            DropboxRefreshToken = dropboxRefreshToken
        });
        return context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AlbumSource albumSource)
    {
        albumSource.LastUpdateDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task DeleteAlbumSourceAsync(AlbumSource albumSource)
    {
        albumSource.DeletedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }
}