using System;
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
using SmallPhotos.Service.BackgroundServices;

namespace SmallPhotos.Service.Tests
{
    public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Startup>
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<SqliteDataContext> _options;
        private readonly Func<IServiceCollection, IServiceCollection> _testServiceConfiguration;

        public IntegrationTestWebApplicationFactory(Func<IServiceCollection, IServiceCollection>? testServiceConfiguration = null)
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _options = new DbContextOptionsBuilder<SqliteDataContext>().UseSqlite(_connection).Options;
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
                        .Replace(ServiceDescriptor.Scoped<SqliteDataContext>(_ => new SqliteDataContext(_options))))))
                .UseSerilog();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
                _connection.Dispose();
        }
    }
}