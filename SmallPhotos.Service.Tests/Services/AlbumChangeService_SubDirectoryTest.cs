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
public class AlbumChangeService_SubDirectoryTest
{
    private readonly IntegrationTestWebApplicationFactory _factory;
    private UserAccount? _userAccount;
    private AlbumSource? _albumSource;
    private string? _albumSourceFolder;

    public AlbumChangeService_SubDirectoryTest() => _factory = new(ConfigureTestServices);

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
        _albumSource = context.AlbumSources!.Add(new() { UserAccount = _userAccount, Folder = _albumSourceFolder, RecurseSubFolders = true }).Entity;
        await context.SaveChangesAsync();
    }

    private IServiceCollection ConfigureTestServices(IServiceCollection services)
    {
        Mock<IHttpClientFactory> httpClientFactory = new();
        httpClientFactory.Setup(x => x.CreateClient(Startup.BackgroundServiceHttpClient)).Returns(() => _factory.CreateClient());
        return services.Replace(ServiceDescriptor.Transient<IHttpClientFactory>(_ => httpClientFactory.Object));
    }

    [TestMethod]
    public async Task Should_add_all_photos_from_subdirectories()
    {
        {
            Directory.CreateDirectory(Path.Combine(_albumSourceFolder ?? "", "subdir1"));
            Directory.CreateDirectory(Path.Combine(_albumSourceFolder ?? "", "subdir2"));
            {
                using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 15, 10);
                await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", "test1.jpg"), MagickFormat.Jpeg);
            }
            {
                using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 25, 20);
                await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", "subdir1", "test1.jpg"), MagickFormat.Jpeg);
            }
            {
                using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 35, 30);
                await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", "subdir2", "test1.jpg"), MagickFormat.Jpeg);
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
            var newPhoto = await context.Photos!.SingleAsync(p => p.Filename == "test1.jpg" && string.IsNullOrEmpty(p.RelativePath));
            newPhoto.AlbumSourceId.Should().Be(_albumSource!.AlbumSourceId);
            newPhoto.Width.Should().Be(15);
            newPhoto.Height.Should().Be(10);
            newPhoto.DateTaken.Should().BeNull();
            newPhoto.DeletedDateTime.Should().BeNull();
            (await context.Thumbnails!.CountAsync(t => t.PhotoId == newPhoto.PhotoId)).Should().Be(3);

            newPhoto = await context.Photos!.SingleAsync(p => p.Filename == "test1.jpg" && p.RelativePath == "subdir1");
            newPhoto.AlbumSourceId.Should().Be(_albumSource!.AlbumSourceId);
            newPhoto.Width.Should().Be(25);
            newPhoto.Height.Should().Be(20);
            newPhoto.DateTaken.Should().BeNull();
            newPhoto.DeletedDateTime.Should().BeNull();
            (await context.Thumbnails!.CountAsync(t => t.PhotoId == newPhoto.PhotoId)).Should().Be(3);

            newPhoto = await context.Photos!.SingleAsync(p => p.Filename == "test1.jpg" && p.RelativePath == "subdir2");
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