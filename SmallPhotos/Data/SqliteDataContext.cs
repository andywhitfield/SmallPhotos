using Microsoft.EntityFrameworkCore;
using SmallPhotos.Model;

namespace SmallPhotos.Data
{
    public class SqliteDataContext : DbContext, ISqliteDataContext
    {
        public SqliteDataContext(DbContextOptions<SqliteDataContext> options) : base(options) { }

        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<AlbumSource> AlbumSources { get; set; }
        public DbSet<Photo> Photos { get; set; }

        public void Migrate() => Database.Migrate();
    }
}