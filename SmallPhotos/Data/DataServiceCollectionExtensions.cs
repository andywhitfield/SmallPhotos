using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SmallPhotos.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddDataServices(this IServiceCollection services) =>
        services
            .AddDbContext<SqliteDataContext>((serviceProvider, options) =>
            {
                var sqliteConnectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("SmallPhotos");
                serviceProvider.GetRequiredService<ILogger<SqliteDataContext>>().LogInformation($"Using connection string: {sqliteConnectionString}");
                options.UseSqlite(sqliteConnectionString);
            })
            .AddScoped(sp => (ISqliteDataContext)sp.GetRequiredService<SqliteDataContext>())
            .AddScoped<IUserAccountRepository, UserAccountRepository>()
            .AddScoped<IAlbumRepository, AlbumRepository>()
            .AddScoped<IPhotoRepository, PhotoRepository>()
            .AddScoped<IPhotoReader, PhotoReader>()
            .AddTransient<IContentTypeProvider, FileExtensionContentTypeProvider>();
}