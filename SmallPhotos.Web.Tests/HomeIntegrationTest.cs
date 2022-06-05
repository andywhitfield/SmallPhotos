using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmallPhotos.Data;
using SmallPhotos.Model;
using Xunit;

namespace SmallPhotos.Web.Tests
{
    public class HomeIntegrationTest : IAsyncLifetime
    {
        private readonly IntegrationTestWebApplicationFactory _factory = new IntegrationTestWebApplicationFactory();

        public async Task InitializeAsync()
        {
            using var serviceScope = _factory.Services.CreateScope();
            await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
            context.Migrate();
            var userAccount = await context.UserAccounts!.AddAsync(new UserAccount { AuthenticationUri = "http://test/user/1" });
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Should_display_expected_items()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var response = await client.GetAsync("/");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("Logout");
        }

        public Task DisposeAsync()
        {
            _factory.Dispose();
            return Task.CompletedTask;
        }
    }
}