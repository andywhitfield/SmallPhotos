using System.Net.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SmallPhotos.Data;

namespace SmallPhotos.Web.Tests;

public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Startup>
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<SqliteDataContext> _options;

    public IntegrationTestWebApplicationFactory()
    {
        _connection = new("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<SqliteDataContext>().UseSqlite(_connection).Options;
    }

    protected override IHostBuilder CreateHostBuilder() => Host
        .CreateDefaultBuilder()
        .ConfigureWebHostDefaults(x => x.UseStartup<Startup>().UseTestServer().ConfigureTestServices(services =>
        {
            services.Replace(ServiceDescriptor.Scoped(_ => new SqliteDataContext(_options)));
            services
                .AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestStubAuthHandler>("Test", null);
        }));

    public HttpClient CreateAuthenticatedClient(bool allowAutoRedirect = true)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = allowAutoRedirect });
        client.DefaultRequestHeaders.Authorization = new("Test");
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _connection.Dispose();
    }
}