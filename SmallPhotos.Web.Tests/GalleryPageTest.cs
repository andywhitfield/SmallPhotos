using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmallPhotos.Data;

namespace SmallPhotos.Web.Tests;

[TestClass]
public class GalleryPageTest
{
    private readonly IntegrationTestWebApplicationFactory _factory = new();
    private string? _albumSourceFolder;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        using var serviceScope = _factory.Services.CreateScope();

        _albumSourceFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Console.WriteLine($"Using photo source dir: [{_albumSourceFolder}]");
        Directory.CreateDirectory(_albumSourceFolder);

        var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        context.Migrate();
        var userAccount = context.UserAccounts!.Add(new() { Email = "test-user-1" });
        var album = context.AlbumSources!.Add(new() { CreatedDateTime = DateTime.UtcNow, Folder = _albumSourceFolder, UserAccount = userAccount.Entity });

        using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 15, 10);
        await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", "photo1.jpg"), MagickFormat.Jpeg);

        var photo = context.Photos!.Add(new() { AlbumSource = album.Entity, CreatedDateTime = DateTime.UtcNow, FileCreationDateTime = DateTime.UtcNow, Filename = "photo1.jpg", Height = 10, Width = 15 });
        context.PhotoTags!.AddRange(
            new() { UserAccount = userAccount.Entity, Photo = photo.Entity, Tag = "first-tag" },
            new() { UserAccount = userAccount.Entity, Photo = photo.Entity, Tag = "second-tag" }
        );
        await context.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Should_be_logged_in_and_have_one_photo()
    {
        using var client = _factory.CreateAuthenticatedClient();
        using var response = await client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Logout");
        responseContent.Should().NotContain("You have no photos");
        responseContent.Should().Contain("""<img src="/photo/thumbnail/Small/1/photo1.jpg" """, Exactly.Once());
    }

    [TestMethod]
    public async Task Should_be_able_to_view_single_photo_with_tags()
    {
        using var client = _factory.CreateAuthenticatedClient();
        {
            using var response = await client.GetAsync("/gallery/1/photo1.jpg");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("Logout");
            responseContent.Should().NotContain("You have no photos");
            responseContent.Should().Contain("""<img id="fullimg" src="/photo/1/photo1.jpg" """, Exactly.Once());
            responseContent.Should().Contain("Tags: first-tag, second-tag", Exactly.Once());
            responseContent.Should().Contain(">first-tag</a>", Exactly.Once());
            responseContent.Should().Contain(">second-tag</a>", Exactly.Once());
        }

        // and the photo should be match the ones we created earlier
        {
            using var response = await client.GetAsync("/photo/1/photo1.jpg");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using MagickImage img = new(await response.Content.ReadAsStreamAsync());
            img.Width.Should().Be(15);
            img.Height.Should().Be(10);
        }
    }

    [TestCleanup]
    public Task DisposeAsync()
    {
        _factory.Dispose();

        Console.WriteLine($"Cleaning up photo source dir: [{_albumSourceFolder}]");
        if (!string.IsNullOrEmpty(_albumSourceFolder) && Directory.Exists(_albumSourceFolder))
            Directory.Delete(_albumSourceFolder, true);

        return Task.CompletedTask;
    }
}