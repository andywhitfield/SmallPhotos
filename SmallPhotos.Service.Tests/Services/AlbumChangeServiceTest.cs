using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Service.Services;

namespace SmallPhotos.Service.Tests.Services;

[TestClass]
public class AlbumChangeServiceTest
{
    private readonly IntegrationTestWebApplicationFactory _factory;
    private UserAccount? _userAccount;
    private AlbumSource? _albumSource;
    private string? _albumSourceFolder;

    public AlbumChangeServiceTest() => _factory = new(ConfigureTestServices);

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

    private IServiceCollection ConfigureTestServices(IServiceCollection services)
    {
        Mock<IHttpClientFactory> httpClientFactory = new();
        httpClientFactory.Setup(x => x.CreateClient(Startup.BackgroundServiceHttpClient)).Returns(() => _factory.CreateClient());
        return services.Replace(ServiceDescriptor.Transient<IHttpClientFactory>(_ => httpClientFactory.Object));
    }

    [TestMethod]
    public async Task Should_sync_new_photo()
    {
        using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 15, 10);
        await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", "test.jpg"), MagickFormat.Jpeg);

        {
            using var serviceScope = _factory.Services.CreateScope();
            var albumSyncService = serviceScope.ServiceProvider.GetRequiredService<IAlbumSyncService>();
            await albumSyncService.SyncAllAsync(CancellationToken.None);
        }

        {
            using var serviceScope = _factory.Services.CreateScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
            (await context.Photos!.CountAsync()).Should().Be(1);
            var newPhoto = await context.Photos!.FirstAsync();
            newPhoto.AlbumSourceId.Should().Be(_albumSource!.AlbumSourceId);
            newPhoto.Filename.Should().Be("test.jpg");
            newPhoto.Width.Should().Be(15);
            newPhoto.Height.Should().Be(10);
            newPhoto.DateTaken.Should().BeNull();
            newPhoto.DeletedDateTime.Should().BeNull();
        }
    }

    [TestMethod]
    public async Task Should_remove_old_photo_and_add_new()
    {
        {
            // initial image
            using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 15, 10);
            await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", "test1.jpg"), MagickFormat.Jpeg);
        }

        {
            using var serviceScope = _factory.Services.CreateScope();
            var albumSyncService = serviceScope.ServiceProvider.GetRequiredService<IAlbumSyncService>();
            await albumSyncService.SyncAllAsync(CancellationToken.None);
        }

        {
            using var serviceScope = _factory.Services.CreateScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
            (await context.Photos!.CountAsync()).Should().Be(1);
            var newPhoto = await context.Photos!.SingleAsync();
            newPhoto.AlbumSourceId.Should().Be(_albumSource!.AlbumSourceId);
            newPhoto.Filename.Should().Be("test1.jpg");
            newPhoto.Width.Should().Be(15);
            newPhoto.Height.Should().Be(10);
            newPhoto.DateTaken.Should().BeNull();
            newPhoto.DeletedDateTime.Should().BeNull();
            (await context.Thumbnails!.CountAsync(t => t.PhotoId == newPhoto.PhotoId)).Should().Be(3);
        }

        {
            // test1.jpg has been deleted and test2.jpg and test3.jpg has been created
            File.Delete(Path.Combine(_albumSourceFolder ?? "", "test1.jpg"));
            {
                using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 25, 20);
                await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", "test2.jpg"), MagickFormat.Jpeg);
            }
            {
                using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 35, 30);
                await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", "test3.jpg"), MagickFormat.Jpeg);
            }
        }

        {
            using var serviceScope = _factory.Services.CreateScope();
            var albumSyncService = serviceScope.ServiceProvider.GetRequiredService<IAlbumSyncService>();
            await albumSyncService.SyncAllAsync(CancellationToken.None);
        }

        {
            using var serviceScope = _factory.Services.CreateScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
            (await context.Photos!.CountAsync()).Should().Be(3);
            var newPhoto = await context.Photos!.SingleAsync(p => p.Filename == "test1.jpg");
            newPhoto.AlbumSourceId.Should().Be(_albumSource!.AlbumSourceId);
            newPhoto.Width.Should().Be(15);
            newPhoto.Height.Should().Be(10);
            newPhoto.DateTaken.Should().BeNull();
            newPhoto.DeletedDateTime.Should().NotBeNull();
            (await context.Thumbnails!.CountAsync(t => t.PhotoId == newPhoto.PhotoId)).Should().Be(0);

            newPhoto = await context.Photos!.SingleAsync(p => p.Filename == "test2.jpg");
            newPhoto.AlbumSourceId.Should().Be(_albumSource!.AlbumSourceId);
            newPhoto.Width.Should().Be(25);
            newPhoto.Height.Should().Be(20);
            newPhoto.DateTaken.Should().BeNull();
            newPhoto.DeletedDateTime.Should().BeNull();
            (await context.Thumbnails!.CountAsync(t => t.PhotoId == newPhoto.PhotoId)).Should().Be(3);

            newPhoto = await context.Photos!.SingleAsync(p => p.Filename == "test3.jpg");
            newPhoto.AlbumSourceId.Should().Be(_albumSource!.AlbumSourceId);
            newPhoto.Width.Should().Be(35);
            newPhoto.Height.Should().Be(30);
            newPhoto.DateTaken.Should().BeNull();
            newPhoto.DeletedDateTime.Should().BeNull();
            (await context.Thumbnails!.CountAsync(t => t.PhotoId == newPhoto.PhotoId)).Should().Be(3);
        }
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