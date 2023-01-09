using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using SmallPhotos.Data;

namespace SmallPhotos.Service.Tests;

public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Startup>
{
    private readonly string _tempDbDir;
    private readonly Func<IServiceCollection, IServiceCollection> _testServiceConfiguration;

    public IntegrationTestWebApplicationFactory(Func<IServiceCollection, IServiceCollection>? testServiceConfiguration = null)
    {
        _tempDbDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDbDir);
        Console.WriteLine($"Using directory {_tempDbDir} for the test db");
        _testServiceConfiguration = testServiceConfiguration ?? (s => s);
    }

    protected override IHostBuilder CreateHostBuilder()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Verbose)
            .WriteTo.Console()
            .CreateLogger();

        return Host
            .CreateDefaultBuilder()
            .ConfigureWebHostDefaults(x => x
                .UseStartup<Startup>()
                .UseTestServer()
                .ConfigureTestServices(services => _testServiceConfiguration(services
                    .RemoveAll<IHostedService>()
                    .Replace(ServiceDescriptor.Scoped<SqliteDataContext>(_ => new SqliteDataContext(new DbContextOptionsBuilder<SqliteDataContext>().UseSqlite(new SqliteConnection($"Data Source={Path.Combine(_tempDbDir, "test.db")}")).Options))))))
            .UseSerilog();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        Console.WriteLine($"Cleaning up test db directory: [{_tempDbDir}]");
        if (!string.IsNullOrEmpty(_tempDbDir) && Directory.Exists(_tempDbDir))
            Directory.Delete(_tempDbDir, true);
    }
}