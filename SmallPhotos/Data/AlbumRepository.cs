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

        public Task<AlbumSource> GetAlbumSourceAsync(UserAccount user, int albumSourceId) =>
            _context
                .AlbumSources
                .FirstOrDefaultAsync(
                    a => a.UserAccountId == user.UserAccountId &&
                    a.AlbumSourceId == albumSourceId &&
                    a.DeletedDateTime == null);

        public Task<List<AlbumSource>> GetAllSourcesAsync(UserAccount user) =>
            _context
                .AlbumSources
                .Where(a => a.UserAccountId == user.UserAccountId && a.DeletedDateTime == null)
                .ToListAsync();

        public async Task AddAlbumSourceAsync(UserAccount userAccount, string folder)
        {
            await _context.AlbumSources.AddAsync(new AlbumSource
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