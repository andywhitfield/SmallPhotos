using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Service.Models;

namespace SmallPhotos.Service.Tests.Controllers;

[TestClass]
public class PhotoControllerTest
{
    private readonly IntegrationTestWebApplicationFactory _factory = new();
    private UserAccount? _userAccount;
    private AlbumSource? _albumSource;
    private string? _albumSourceFolder;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        using var serviceScope = _factory.Services.CreateScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        context.Migrate();

        _albumSourceFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Console.WriteLine($"Using photo source dir: [{_albumSourceFolder}]");
        Directory.CreateDirectory(_albumSourceFolder);

        _userAccount = context.UserAccounts!.Add(new() { Email = "test-user-1" }).Entity;
        _albumSource = context.AlbumSources!.Add(new() { UserAccount = _userAccount, Folder = _albumSourceFolder }).Entity;
        await context.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Should_save_new_photo()
    {
        using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 15, 10);
        await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", "test.jpg"), MagickFormat.Jpeg);

        CreateOrUpdatePhotoRequest request = new()
        {
            UserAccountId = _userAccount!.UserAccountId,
            AlbumSourceId = _albumSource!.AlbumSourceId,
            Filename = "test.jpg"
        };

        using var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/photo", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
        var responseContent = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: $"request is valid but failed: '{responseContent}'");
        var responsePhoto = JsonSerializer.Deserialize<Photo>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        responsePhoto.Should().NotBeNull();
        responsePhoto!.Filename.Should().Be("test.jpg");
        responsePhoto.Width.Should().Be(15);
        responsePhoto.Height.Should().Be(10);
        responsePhoto.DateTaken.Should().BeNull();
        responsePhoto.Latitude.Should().BeNull();
        responsePhoto.Longitude.Should().BeNull();

        // check saved photo
        using var serviceScope = _factory.Services.CreateScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        (await context.Photos!.CountAsync()).Should().Be(1);
        var newPhoto = await context.Photos!.FirstAsync();
        newPhoto.AlbumSourceId.Should().Be(_albumSource.AlbumSourceId);
        newPhoto.Filename.Should().Be("test.jpg");
        newPhoto.Width.Should().Be(15);
        newPhoto.Height.Should().Be(10);
        newPhoto.DateTaken.Should().BeNull();
        newPhoto.Latitude.Should().BeNull();
        newPhoto.Longitude.Should().BeNull();

        // check thumbnails
        (await context.Thumbnails!.CountAsync()).Should().Be(3);
        var thumbnail = await context.Thumbnails!.FirstOrDefaultAsync(t => t.ThumbnailSize == ThumbnailSize.Small);
        thumbnail.Should().NotBeNull();
        thumbnail!.PhotoId.Should().Be(newPhoto.PhotoId);
        thumbnail.ThumbnailImage.Should().BeOfSize(ThumbnailSize.Small.ToSize());

        thumbnail = await context.Thumbnails!.FirstOrDefaultAsync(t => t.ThumbnailSize == ThumbnailSize.Medium);
        thumbnail.Should().NotBeNull();
        thumbnail!.PhotoId.Should().Be(newPhoto.PhotoId);
        thumbnail.ThumbnailImage.Should().BeOfSize(ThumbnailSize.Medium.ToSize());

        thumbnail = await context.Thumbnails!.FirstOrDefaultAsync(t => t.ThumbnailSize == ThumbnailSize.Large);
        thumbnail.Should().NotBeNull();
        thumbnail!.PhotoId.Should().Be(newPhoto.PhotoId);
        thumbnail.ThumbnailImage.Should().BeOfSize(ThumbnailSize.Large.ToSize());
    }

    [TestMethod]
    public async Task Should_update_existing_photo()
    {
        Photo newPhoto;
        {
            // create and upload an image
            using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 15, 10);
            ExifProfile profile = new();
            profile.SetValue(ExifTag.DateTimeOriginal, "2022:09:19 13:20:10");
            img.SetProfile(profile);
            await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", "test.jpg"), MagickFormat.Jpeg);

            CreateOrUpdatePhotoRequest request = new()
            {
                UserAccountId = _userAccount!.UserAccountId,
                AlbumSourceId = _albumSource!.AlbumSourceId,
                Filename = "test.jpg"
            };

            using var client = _factory.CreateClient();
            var response = await client.PostAsync("/api/photo", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
            var responseContent = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, because: $"request is valid but failed: '{responseContent}'");
            var responsePhoto = JsonSerializer.Deserialize<Photo>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            responsePhoto.Should().NotBeNull();
            responsePhoto!.Filename.Should().Be("test.jpg");
            responsePhoto.Width.Should().Be(15);
            responsePhoto.Height.Should().Be(10);
            responsePhoto.DateTaken.Should().Be(new DateTime(2022, 9, 19, 13, 20, 10));
            responsePhoto.Latitude.Should().BeNull();
            responsePhoto.Longitude.Should().BeNull();

            using var serviceScope = _factory.Services.CreateScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
            (await context.Photos!.CountAsync()).Should().Be(1);
            newPhoto = await context.Photos!.FirstAsync();
            newPhoto.LastUpdateDateTime.Should().BeNull();
            newPhoto.DateTaken.Should().Be(new DateTime(2022, 9, 19, 13, 20, 10));
            newPhoto.Latitude.Should().BeNull();
            newPhoto.Longitude.Should().BeNull();
        }

        {
            // the image has been updated - made twice as wide
            using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 30, 10);
            await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", "test.jpg"), MagickFormat.Jpeg);

            CreateOrUpdatePhotoRequest request = new()
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
            responsePhoto.Should().NotBeNull();
            responsePhoto!.Filename.Should().Be("test.jpg");
            responsePhoto.Width.Should().Be(30);
            responsePhoto.Height.Should().Be(10);
            responsePhoto.DateTaken.Should().BeNull();
            responsePhoto.Latitude.Should().BeNull();
            responsePhoto.Longitude.Should().BeNull();
        }

        Photo updatedPhoto;
        {
            using var serviceScope = _factory.Services.CreateScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
            (await context.Photos!.CountAsync()).Should().Be(1);
            updatedPhoto = await context.Photos!.FirstAsync();
        }
        updatedPhoto.PhotoId.Should().Be(newPhoto.PhotoId);
        updatedPhoto.CreatedDateTime.Should().Be(newPhoto.CreatedDateTime);
        updatedPhoto.AlbumSourceId.Should().Be(_albumSource.AlbumSourceId);
        updatedPhoto.Filename.Should().Be("test.jpg");
        updatedPhoto.Width.Should().Be(30);
        updatedPhoto.Height.Should().Be(10);
        updatedPhoto.DateTaken.Should().BeNull();
        updatedPhoto.Latitude.Should().BeNull();
        updatedPhoto.Longitude.Should().BeNull();
        updatedPhoto.FileCreationDateTime.Should().Be(newPhoto.FileCreationDateTime);
        updatedPhoto.FileModificationDateTime.Should().BeAfter(newPhoto.FileModificationDateTime);
        updatedPhoto.LastUpdateDateTime.Should().NotBeNull();

        {
            using var serviceScope = _factory.Services.CreateScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();

            // check thumbnails
            (await context.Thumbnails!.CountAsync()).Should().Be(3);
            var thumbnail = await context.Thumbnails!.FirstOrDefaultAsync(t => t.ThumbnailSize == ThumbnailSize.Small);
            thumbnail.Should().NotBeNull();
            thumbnail!.PhotoId.Should().Be(updatedPhoto.PhotoId);
            thumbnail.ThumbnailImage.Should().BeOfSize(ThumbnailSize.Small.ToSize());

            thumbnail = await context.Thumbnails!.FirstOrDefaultAsync(t => t.ThumbnailSize == ThumbnailSize.Medium);
            thumbnail.Should().NotBeNull();
            thumbnail!.PhotoId.Should().Be(updatedPhoto.PhotoId);
            thumbnail.ThumbnailImage.Should().BeOfSize(ThumbnailSize.Medium.ToSize());

            thumbnail = await context.Thumbnails!.FirstOrDefaultAsync(t => t.ThumbnailSize == ThumbnailSize.Large);
            thumbnail.Should().NotBeNull();
            thumbnail!.PhotoId.Should().Be(updatedPhoto.PhotoId);
            thumbnail.ThumbnailImage.Should().BeOfSize(ThumbnailSize.Large.ToSize());
        }
    }

    [TestMethod]
    public async Task Should_save_image_with_gps_data()
    {
        async Task WriteResourceToFileAsync()
        {
            await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SmallPhotos.Service.Tests.Controllers.2026-09-25 08.24.35.jpg");
            Assert.IsNotNull(stream);
            await using var outputFile = File.Create(Path.Combine(_albumSourceFolder ?? "", "test.jpg"));
            await stream.CopyToAsync(outputFile);
        }
        await WriteResourceToFileAsync();

        CreateOrUpdatePhotoRequest request = new()
        {
            UserAccountId = _userAccount!.UserAccountId,
            AlbumSourceId = _albumSource!.AlbumSourceId,
            Filename = "test.jpg"
        };

        using var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/photo", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
        var responseContent = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: $"request is valid but failed: '{responseContent}'");
        var responsePhoto = JsonSerializer.Deserialize<Photo>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        responsePhoto.Should().NotBeNull();
        responsePhoto!.Filename.Should().Be("test.jpg");
        responsePhoto.Width.Should().Be(4284);
        responsePhoto.Height.Should().Be(5712);
        responsePhoto.DateTaken.Should().Be(new(2026, 9, 25, 8, 24, 34));
        responsePhoto.Latitude.Should().Be(51.80801666666667);
        responsePhoto.Longitude.Should().Be(-0.19811944444444443);

        // check saved photo
        using var serviceScope = _factory.Services.CreateScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        (await context.Photos!.CountAsync()).Should().Be(1);
        var newPhoto = await context.Photos!.FirstAsync();
        newPhoto.AlbumSourceId.Should().Be(_albumSource.AlbumSourceId);
        newPhoto.Filename.Should().Be("test.jpg");
        newPhoto.Width.Should().Be(4284);
        newPhoto.Height.Should().Be(5712);
        newPhoto.DateTaken.Should().Be(new(2026, 9, 25, 8, 24, 34));
        newPhoto.Latitude.Should().Be(51.80801666666667);
        newPhoto.Longitude.Should().Be(-0.19811944444444443);

        // check thumbnails
        (await context.Thumbnails!.CountAsync()).Should().Be(3);
        var thumbnail = await context.Thumbnails!.FirstOrDefaultAsync(t => t.ThumbnailSize == ThumbnailSize.Small);
        thumbnail.Should().NotBeNull();
        thumbnail!.PhotoId.Should().Be(newPhoto.PhotoId);
        thumbnail.ThumbnailImage.Should().BeOfSize(ThumbnailSize.Small.ToSize());

        thumbnail = await context.Thumbnails!.FirstOrDefaultAsync(t => t.ThumbnailSize == ThumbnailSize.Medium);
        thumbnail.Should().NotBeNull();
        thumbnail!.PhotoId.Should().Be(newPhoto.PhotoId);
        thumbnail.ThumbnailImage.Should().BeOfSize(ThumbnailSize.Medium.ToSize());

        thumbnail = await context.Thumbnails!.FirstOrDefaultAsync(t => t.ThumbnailSize == ThumbnailSize.Large);
        thumbnail.Should().NotBeNull();
        thumbnail!.PhotoId.Should().Be(newPhoto.PhotoId);
        thumbnail.ThumbnailImage.Should().BeOfSize(ThumbnailSize.Large.ToSize());
    }

    [TestCleanup]
    public Task DisposeAsync()
    {
        Console.WriteLine($"Cleaning up photo source dir: [{_albumSourceFolder}]");
        if (!string.IsNullOrEmpty(_albumSourceFolder) && Directory.Exists(_albumSourceFolder))
            Directory.Delete(_albumSourceFolder, true);

        _factory.Dispose();
        return Task.CompletedTask;
    }
}