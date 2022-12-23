using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SmallPhotos.Data;
using SmallPhotos.Model;
using Xunit;

namespace SmallPhotos.Web.Tests
{
    public class EmptyHomePageTest : IAsyncLifetime
    {
        private readonly IntegrationTestWebApplicationFactory _factory = new IntegrationTestWebApplicationFactory();

        public async Task InitializeAsync()
        {
            using var serviceScope = _factory.Services.CreateScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
            context.Migrate();
            var userAccount = context.UserAccounts!.Add(new UserAccount { AuthenticationUri = "http://test/user/1" });
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Should_be_logged_in_and_have_no_photos()
        {
            using var client = _factory.CreateAuthenticatedClient();
            using var response = await client.GetAsync("/");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("Logout");
            responseContent.Should().Contain("You have no photos");
        }

        public Task DisposeAsync()
        {
            _factory.Dispose();
            return Task.CompletedTask;
        }
    }
}