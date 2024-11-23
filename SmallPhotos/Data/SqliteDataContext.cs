using Microsoft.EntityFrameworkCore;
using SmallPhotos.Model;

namespace SmallPhotos.Data;

public class SqliteDataContext(DbContextOptions<SqliteDataContext> options)
    : DbContext(options), ISqliteDataContext
{
    public DbSet<UserAccount>? UserAccounts { get; set; }
    public DbSet<UserAccountCredential>? UserAccountCredentials { get; set; }
    public DbSet<AlbumSource>? AlbumSources { get; set; }
    public DbSet<Photo>? Photos { get; set; }
    public DbSet<Thumbnail>? Thumbnails { get; set; }
    public DbSet<StarredPhoto>? StarredPhotos { get; set; }
    public DbSet<PhotoTag>? PhotoTags { get; set; }

    public void Migrate() => Database.Migrate();
}