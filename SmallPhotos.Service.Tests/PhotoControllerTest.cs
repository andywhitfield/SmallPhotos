using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Service.Models;
using Xunit;

namespace SmallPhotos.Service.Tests
{
    public class PhotoControllerTest : IAsyncLifetime
    {
        private readonly IntegrationTestWebApplicationFactory _factory = new IntegrationTestWebApplicationFactory();
        private UserAccount _userAccount;
        private AlbumSource _albumSource;
        private string _albumSourceFolder;

        public async Task InitializeAsync()
        {
            using var serviceScope = _factory.Services.CreateScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
            context.Migrate();

            _albumSourceFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Console.WriteLine($"Using photo source dir: [{_albumSourceFolder}]");
            Directory.CreateDirectory(_albumSourceFolder);

            _userAccount = (await context.UserAccounts.AddAsync(new UserAccount { AuthenticationUri = "http://test/user/1" })).Entity;
            _albumSource = (await context.AlbumSources.AddAsync(new AlbumSource { UserAccount = _userAccount, Folder = _albumSourceFolder })).Entity;
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Should_save_new_photo()
        {
            var img = new MagickImage(new MagickColor(ushort.MaxValue, 0, 0), 15, 10);
            await img.WriteAsync(Path.Combine(_albumSourceFolder, "test.jpg"), MagickFormat.Jpeg);
            
            var request = new CreateOrUpdatePhotoRequest
            {
                UserAccountId = _userAccount.UserAccountId,
                AlbumSourceId = _albumSource.AlbumSourceId,
                Filename = "test.jpg"
            };

            using var client = _factory.CreateClient();
            var response = await client.PostAsync("/api/photo", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
            var responseContent = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, because: $"request is valid but failed: '{responseContent}'");
            var responsePhoto = JsonSerializer.Deserialize<Photo>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            responsePhoto.Filename.Should().Be("test.jpg");
            responsePhoto.Width.Should().Be(15);
            responsePhoto.Height.Should().Be(10);
        }

        public Task DisposeAsync()
        {
            Console.WriteLine($"Cleaning up photo source dir: [{_albumSourceFolder}]");
            //if (!string.IsNullOrEmpty(_albumSourceFolder) && Directory.Exists(_albumSourceFolder))
                //Directory.Delete(_albumSourceFolder, true);

            _factory.Dispose();
            return Task.CompletedTask;
        }
    }
}