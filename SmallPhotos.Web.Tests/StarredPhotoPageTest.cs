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
public class StarredPhotoPageTest
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

        {
            using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 15, 10);
            await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", "photo1.jpg"), MagickFormat.Jpeg);
        }
        {
            using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 25, 20);
            await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", "photo2.jpg"), MagickFormat.Jpeg);
        }

        var photo = context.Photos!.Add(new() { AlbumSource = album.Entity, CreatedDateTime = DateTime.UtcNow, FileCreationDateTime = DateTime.UtcNow, Filename = "photo1.jpg", Height = 10, Width = 15 });
        photo = context.Photos!.Add(new() { AlbumSource = album.Entity, CreatedDateTime = DateTime.UtcNow, FileCreationDateTime = DateTime.UtcNow, Filename = "photo2.jpg", Height = 20, Width = 25 });
        // only the second photo is starred
        context.StarredPhotos!.Add(new() { UserAccount = userAccount.Entity, Photo = photo.Entity, CreatedDateTime = DateTime.UtcNow });
        await context.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Should_show_all_starred_photos()
    {
        using var client = _factory.CreateAuthenticatedClient();
        using var response = await client.GetAsync("/starred");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Logout");
        responseContent.Should().NotContain("You have no starred photos");
        responseContent.Should().NotContain("""<img src="/photo/thumbnail/Small/1/photo1.jpg" """);
        responseContent.Should().Contain("""<img src="/photo/thumbnail/Small/2/photo2.jpg" """, Exactly.Once());
        responseContent.Should().Contain("from=starred", Exactly.Once());
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