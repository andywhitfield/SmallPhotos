using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using SmallPhotos.Data;
using SmallPhotos.Model;
using Xunit;

namespace SmallPhotos.Web.Tests;

public class PhotosInSubDirectoryTest : IAsyncLifetime
{
    private readonly IntegrationTestWebApplicationFactory _factory = new IntegrationTestWebApplicationFactory();
    private string? _albumSourceFolder;

    public async Task InitializeAsync()
    {
        using var serviceScope = _factory.Services.CreateScope();

        _albumSourceFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Console.WriteLine($"Using photo source dir: [{_albumSourceFolder}]");
        Directory.CreateDirectory(_albumSourceFolder);

        const string subdir1 = "yesterday";
        const string subdir2 = "today";
        Directory.CreateDirectory(Path.Combine(_albumSourceFolder, subdir1));
        Directory.CreateDirectory(Path.Combine(_albumSourceFolder, subdir2));

        var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        context.Migrate();
        var userAccount = context.UserAccounts!.Add(new UserAccount { AuthenticationUri = "http://test/user/1" });
        var album = context.AlbumSources!.Add(new AlbumSource { CreatedDateTime = DateTime.UtcNow, Folder = _albumSourceFolder, UserAccount = userAccount.Entity });

        {
            using var img = new MagickImage(new MagickColor(ushort.MaxValue, 0, 0), 15, 10);
            await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", subdir1, "photo1.jpg"), MagickFormat.Jpeg);
        }

        {
            using var img = new MagickImage(new MagickColor(ushort.MaxValue, 0, 0), 25, 20);
            await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", subdir2, "photo1.jpg"), MagickFormat.Jpeg);
        }

        context.Photos!.Add(new Photo { AlbumSource = album.Entity, CreatedDateTime = DateTime.UtcNow, FileCreationDateTime = DateTime.UtcNow, Filename = "photo1.jpg", RelativePath = subdir1, Height = 10, Width = 15 });
        context.Photos!.Add(new Photo { AlbumSource = album.Entity, CreatedDateTime = DateTime.UtcNow, FileCreationDateTime = DateTime.UtcNow, Filename = "photo1.jpg", RelativePath = subdir2, Height = 20, Width = 25 });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Should_be_logged_in_and_have_two_photos()
    {
        using var client = _factory.CreateAuthenticatedClient();
        using var response = await client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Logout");
        responseContent.Should().NotContain("You have no photos");
        responseContent.Should().Contain("""<img src="/photo/thumbnail/Small/1/photo1.jpg" """, Exactly.Once());
        responseContent.Should().Contain("""<img src="/photo/thumbnail/Small/2/photo1.jpg" """, Exactly.Once());
    }

    [Fact]
    public async Task Should_be_able_to_view_single_photo()
    {
        using var client = _factory.CreateAuthenticatedClient();
        {
            using var response = await client.GetAsync("/gallery/1/photo1.jpg");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("Logout");
            responseContent.Should().NotContain("You have no photos");
            responseContent.Should().Contain("""<img id="fullimg" src="/photo/1/photo1.jpg" """, Exactly.Once());
        }
        {
            using var response = await client.GetAsync("/gallery/2/photo1.jpg");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("Logout");
            responseContent.Should().NotContain("You have no photos");
            responseContent.Should().Contain("""<img id="fullimg" src="/photo/2/photo1.jpg" """, Exactly.Once());
        }

        // and the photos should be match the ones we created earlier
        {
            using var response = await client.GetAsync("/photo/1/photo1.jpg");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var img = new MagickImage(await response.Content.ReadAsStreamAsync());
            img.Width.Should().Be(15);
            img.Height.Should().Be(10);
        }
        {
            using var response = await client.GetAsync("/photo/2/photo1.jpg");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var img = new MagickImage(await response.Content.ReadAsStreamAsync());
            img.Width.Should().Be(25);
            img.Height.Should().Be(20);
        }
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();

        Console.WriteLine($"Cleaning up photo source dir: [{_albumSourceFolder}]");
        if (!string.IsNullOrEmpty(_albumSourceFolder) && Directory.Exists(_albumSourceFolder))
            Directory.Delete(_albumSourceFolder, true);

        return Task.CompletedTask;
    }
}