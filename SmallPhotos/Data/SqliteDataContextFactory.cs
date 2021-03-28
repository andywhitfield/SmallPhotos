using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmallPhotos.Data
{
    // used by the migrations tool only
    public class SqliteDataContextFactory : IDesignTimeDbContextFactory<SqliteDataContext>
    {
        public SqliteDataContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SqliteDataContext>();
            optionsBuilder.UseSqlite("Data Source=SmallPhotos.Web/smallphotos.db");
            return new SqliteDataContext(optionsBuilder.Options);
        }
    }
}