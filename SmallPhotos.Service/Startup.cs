﻿using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SmallPhotos.Data;
using SmallPhotos.Service.BackgroundServices;
using SmallPhotos.Service.Services;

namespace SmallPhotos.Service
{
    public class Startup
    {
        public const string BackgroundServiceHttpClient = "BackgroundServiceHttpClient";

        private IWebHostEnvironment _hostingEnvironment;
        private IFeatureCollection _featureCollection;

        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            _hostingEnvironment = env;
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConfiguration>(Configuration);

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
                    var serviceAddress = _featureCollection.Get<IServerAddressesFeature>().Addresses.First();
                    logger.LogDebug($"Creating HttpClient[{BackgroundServiceHttpClient}] with address [{serviceAddress}]");
                    cfg.BaseAddress = new Uri(serviceAddress);
                });
            services.AddMvc();
            services.AddCors();

            services.Configure<AlbumChangeServiceOptions>(Configuration.GetSection("AlbumChangeService"));
            services.AddScoped<IAlbumSyncService, AlbumSyncService>();
            services.AddHostedService<AlbumChangeService>();
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
}
