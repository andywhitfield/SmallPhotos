using System.Threading.RateLimiting;
using BigDataCloud;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Serilog;
using SmallPhotos.Data;
using SmallPhotos.Dropbox;
using SmallPhotos.Service.BackgroundServices;
using SmallPhotos.Service.Services;

namespace SmallPhotos.Service;

public class Startup
{
    public const string BackgroundServiceHttpClient = nameof(BackgroundServiceHttpClient);
    public const string BigDataCloudPollyPolicy = nameof(BigDataCloudPollyPolicy);

    private IFeatureCollection? _featureCollection;

    public Startup(IWebHostEnvironment env)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();
        Configuration = builder.Build();
    }

    public IConfigurationRoot Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IConfiguration>(Configuration);
        services.AddSingleton(TimeProvider.System);

        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddDebug();
        });

        services
            .AddDataServices()
            .AddTransient<IThumbnailCreator, ThumbnailCreator>()
            .AddHttpClient(BackgroundServiceHttpClient, (provider, cfg) =>
            {
                var logger = provider.GetRequiredService<ILogger<Startup>>();
                var serviceAddress = _featureCollection?.Get<IServerAddressesFeature>()?.Addresses?.FirstOrDefault();
                if (serviceAddress == null)
                {
                    logger.LogCritical("Cannot get service address - background service will not be able to run successfully!");
                    provider.GetService<IHostApplicationLifetime>()?.StopApplication();
                    return;
                }
                logger.LogDebug("Creating HttpClient[{BackgroundServiceHttpClient}] with address [{ServiceAddress}]", BackgroundServiceHttpClient, serviceAddress);
                cfg.BaseAddress = new(serviceAddress);
            });
        services.AddMvc();
        services.AddCors();

        services.Configure<AlbumChangeServiceOptions>(Configuration.GetSection("AlbumChangeService"));
        services.Configure<GeoLocationServiceOptions>(Configuration.GetSection("GeoLocationService"));
        services.Configure<DropboxOptions>(Configuration.GetSection("Dropbox"));
        services.AddScoped<IAlbumSyncService, AlbumSyncService>();
        services.AddScoped<IFilesystemSync, FilesystemSync>();
        services.AddScoped<IDropboxSync, DropboxSync>();
        services.AddScoped<IDropboxClientProxy, DropboxClientProxy>();
        services.AddScoped<IGeoLocationUpdateService, GeoLocationUpdateService>();
        services.AddScoped(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Startup>>();
            var geoLocationServiceOptions = sp.GetRequiredService<IOptionsSnapshot<GeoLocationServiceOptions>>().Value;
            if (geoLocationServiceOptions.Enabled && string.IsNullOrEmpty(geoLocationServiceOptions.ApiKey))
            {
                logger.LogCritical("Cannot get GeoLocation API Key - background service will not be able to run successfully!");
                sp.GetService<IHostApplicationLifetime>()?.StopApplication();
                return default!;
            }

            logger.LogDebug("Creating BigDataCloud client with api key length {ApiKeyLength}", geoLocationServiceOptions.ApiKey.Length);
            return new BigDataCloudClient(geoLocationServiceOptions.ApiKey);
        });
        services.AddScoped<RateLimiter>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Startup>>();
            var geoLocationServiceOptions = sp.GetRequiredService<IOptionsSnapshot<GeoLocationServiceOptions>>().Value;
            logger.LogDebug("Creating BigDataCloud rate limiter, limit={RateLimit} window={RateLimitWindow}", geoLocationServiceOptions.RateLimit, geoLocationServiceOptions.RateLimitWindow);
            return new SlidingWindowRateLimiter(new()
            {
                PermitLimit = geoLocationServiceOptions.RateLimit,
                Window = geoLocationServiceOptions.RateLimitWindow,
                SegmentsPerWindow = 1,
                QueueLimit = 1,
                AutoReplenishment = true
            });
        });
        services.AddHostedService<AlbumChangeService>();
        services.AddHostedService<GeoLocationService>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        app.UseSerilogRequestLogging();
        app.UseRouting();
        app.UseEndpoints(options => options.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}"));

        _featureCollection = app.ServerFeatures;

        using var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        scope.ServiceProvider.GetRequiredService<ISqliteDataContext>().Migrate();
    }
}
