using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmallPhotos.Data;

// used by the migrations tool only
public class SqliteDataContextFactory : IDesignTimeDbContextFactory<SqliteDataContext>
{
    public SqliteDataContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<SqliteDataContext> optionsBuilder = new();
        optionsBuilder.UseSqlite(":memory:");
        return new(optionsBuilder.Options);
    }
}