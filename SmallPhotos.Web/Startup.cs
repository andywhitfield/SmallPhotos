using System;
using System.IO;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Dropbox;

namespace SmallPhotos.Web;

public class Startup
{
    public Startup(IWebHostEnvironment env)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();
        Configuration = builder.Build();
        Environment = env;
    }

    public IConfiguration Configuration { get; }
    public IWebHostEnvironment Environment { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Configuration);

        services
            .AddAuthentication(o => o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o =>
            {
                o.LoginPath = "/signin";
                o.LogoutPath = "/signout";
                o.Cookie.HttpOnly = true;
                o.Cookie.MaxAge = TimeSpan.FromDays(7);
                o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                o.Cookie.IsEssential = true;
                o.ExpireTimeSpan = TimeSpan.FromDays(7);
                o.SlidingExpiration = true;
            });

        services
            .AddDataProtection()
            .SetApplicationName(typeof(Startup).Namespace ?? "")
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Environment.ContentRootPath, ".keys")));

        services.AddLogging(logging =>
        {
            logging.AddSimpleConsole(opt =>
            {
                opt.UseUtcTimestamp = true;
                opt.TimestampFormat = "[HH:mm:ss.fff] ";
                opt.SingleLine = true;
            });
            logging.AddDebug();
            logging.SetMinimumLevel(LogLevel.Trace);
        });

        services.AddScoped<IDropboxClientProxy, DropboxClientProxy>();
        services.Configure<DropboxOptions>(Configuration.GetSection("Dropbox"));

        services.Configure<CookiePolicyOptions>(o =>
        {
            o.CheckConsentNeeded = context => false;
            o.MinimumSameSitePolicy = SameSiteMode.None;
        });

        services.AddDataServices();
        services.AddMediatR(typeof(Startup));
        services.AddMvc().AddSessionStateTempDataProvider();
        var builder = services.AddRazorPages();
#if DEBUG
        if (Environment.IsDevelopment())
            builder.AddRazorRuntimeCompilation();
#endif
        services.AddCors();
        services.AddDistributedMemoryCache();
        services
            .AddSession(options => options.IdleTimeout = TimeSpan.FromMinutes(5))
            .AddFido2(options =>
            {
                options.ServerName = "Small:Photos";
                options.ServerDomain = Configuration.GetValue<string>("FidoDomain");
                options.Origins = [Configuration.GetValue<string>("FidoOrigins")];
            });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();
        else
            app.UseExceptionHandler("/Home/Error");

        app.UseStaticFiles();
        app.UseCookiePolicy();
        app.UseSession();
        app.UseAuthentication();
        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(options => options.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}"));

        using var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        scope.ServiceProvider.GetRequiredService<ISqliteDataContext>().Migrate();
    }
}
