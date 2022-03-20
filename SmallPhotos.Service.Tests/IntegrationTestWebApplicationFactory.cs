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

namespace SmallPhotos.Service.Tests
{
    public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Startup>
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<SqliteDataContext> _options;

        public IntegrationTestWebApplicationFactory()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _options = new DbContextOptionsBuilder<SqliteDataContext>().UseSqlite(_connection).Options;
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
                    .ConfigureTestServices(services => services
                        .Replace(ServiceDescriptor.Scoped<SqliteDataContext>(_ => new SqliteDataContext(_options)))))
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