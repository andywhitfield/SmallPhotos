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
public class TaggedPhotoPageTest
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
        context.PhotoTags!.AddRange(
            new() { UserAccount = userAccount.Entity, Photo = photo.Entity, Tag = "test-tag-1" },
            new() { UserAccount = userAccount.Entity, Photo = photo.Entity, Tag = "test-tag-2" }
        );

        photo = context.Photos!.Add(new() { AlbumSource = album.Entity, CreatedDateTime = DateTime.UtcNow, FileCreationDateTime = DateTime.UtcNow, Filename = "photo2.jpg", Height = 20, Width = 25 });
        context.PhotoTags!.AddRange(
            new() { UserAccount = userAccount.Entity, Photo = photo.Entity, Tag = "test-tag-2" },
            new() { UserAccount = userAccount.Entity, Photo = photo.Entity, Tag = "test-tag-3" }
        );

        await context.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Should_list_all_tags()
    {
        using var client = _factory.CreateAuthenticatedClient();
        using var response = await client.GetAsync("/tagged");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Logout");
        responseContent.Should().NotContain("You have no photos with tags");
        responseContent.Should().Contain(""" title="Tag: test-tag-1" """, Exactly.Once());
        responseContent.Should().Contain(""" title="Tag: test-tag-2" """, Exactly.Once());
        responseContent.Should().Contain(""" title="Tag: test-tag-3" """, Exactly.Once());
        responseContent.Should().Contain("test-tag-2 (2)", Exactly.Once());
        responseContent.Should().Contain("test-tag-1 (1)", Exactly.Once());
        responseContent.Should().Contain("test-tag-3 (1)", Exactly.Once());
    }

    [TestMethod]
    public async Task Should_show_all_photos_with_tag()
    {
        using var client = _factory.CreateAuthenticatedClient();
        using var response = await client.GetAsync("/tagged/test-tag-2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Logout");
        responseContent.Should().NotContain("You have no photos with tags");
        responseContent.Should().Contain("""<img src="/photo/thumbnail/Small/1/photo1.jpg" """, Exactly.Once());
        responseContent.Should().Contain("""<img src="/photo/thumbnail/Small/2/photo2.jpg" """, Exactly.Once());
        responseContent.Should().Contain("from=tagged_test-tag-2", Exactly.Twice());
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