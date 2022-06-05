using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallPhotos.Model;

namespace SmallPhotos.Data
{
    public class AlbumRepository : IAlbumRepository
    {
        private readonly ILogger<AlbumRepository> _logger;
        private readonly SqliteDataContext _context;

        public AlbumRepository(ILogger<AlbumRepository> logger, SqliteDataContext context)
        {
            _logger = logger;
            _context = context;
        }

        public Task<AlbumSource?> GetAsync(UserAccount user, long albumSourceId) =>
            _context
                .AlbumSources!
                .FirstOrDefaultAsync(a =>
                    a.UserAccountId == user.UserAccountId &&
                    a.AlbumSourceId == albumSourceId &&
                    a.DeletedDateTime == null);

        public Task<List<AlbumSource>> GetAllAsync(UserAccount user) =>
            _context
                .AlbumSources!
                .Where(a => a.UserAccountId == user.UserAccountId && a.DeletedDateTime == null)
                .ToListAsync();

        public async Task AddAsync(UserAccount userAccount, string folder)
        {
            await _context.AlbumSources!.AddAsync(new AlbumSource
            {
                UserAccount = userAccount,
                Folder = folder
            });
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAlbumSourceAsync(AlbumSource albumSource)
        {
            albumSource.DeletedDateTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}