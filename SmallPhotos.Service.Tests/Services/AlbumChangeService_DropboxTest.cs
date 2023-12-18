using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dropbox.Api.Files;
using Dropbox.Api.Stone;
using FluentAssertions;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using SmallPhotos.Data;
using SmallPhotos.Dropbox;
using SmallPhotos.Model;
using SmallPhotos.Service.Services;
using Xunit;

namespace SmallPhotos.Service.Tests.Services;

public class AlbumChangeService_DropboxTest : IAsyncLifetime
{
    private IntegrationTestWebApplicationFactory? _factory;
    private UserAccount? _userAccount;
    private AlbumSource? _albumSource;
    private string? _dropboxTemporaryFolder;
    private readonly Mock<IDropboxClientProxy> _dropboxClientProxy = new();

    public async Task InitializeAsync()
    {
        _dropboxTemporaryFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Console.WriteLine($"Using dropbox temp dir: [{_dropboxTemporaryFolder}]");
        Directory.CreateDirectory(_dropboxTemporaryFolder);

        _factory = new(ConfigureTestServices);
        using var serviceScope = _factory.Services.CreateScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        context.Migrate();

        _userAccount = context.UserAccounts!.Add(new() { Email = "test-user-1" }).Entity;
        _albumSource = context.AlbumSources!.Add(new() { UserAccount = _userAccount, Folder = "/photos", DropboxAccessToken = "test-dropbox-access-token", DropboxRefreshToken = "test-dropbox-refresh-token" }).Entity;
        await context.SaveChangesAsync();
    }

    private IServiceCollection ConfigureTestServices(IServiceCollection services)
    {
        Mock<IHttpClientFactory> httpClientFactory = new();
        httpClientFactory.Setup(x => x.CreateClient(Startup.BackgroundServiceHttpClient)).Returns(() => _factory!.CreateClient());

        _dropboxClientProxy.Setup(x => x.RefreshAccessTokenAsync(new[] { "files.content.read" })).ReturnsAsync(true);
        _dropboxClientProxy.Setup(x => x.TemporaryDownloadDirectory).Returns(new DirectoryInfo(_dropboxTemporaryFolder ?? throw new Exception()));

        return services
            .Replace(ServiceDescriptor.Transient<IHttpClientFactory>(_ => httpClientFactory.Object))
            .Replace(ServiceDescriptor.Scoped<IDropboxClientProxy>(_ => _dropboxClientProxy.Object));
    }

    [Fact]
    public async Task Should_sync_new_photo()
    {
        using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 15, 10);
        await using MemoryStream memoryStream = new();
        await img.WriteAsync(memoryStream, MagickFormat.Jpeg);
        memoryStream.Position = 0;
        FileMetadata testFile = new("test.jpg", "photo-1", DateTime.UtcNow, DateTime.UtcNow, "000000001", 100);
        _dropboxClientProxy.Setup(x => x.ListFolderAsync("/photos", false)).ReturnsAsync(new ListFolderResult(new[] { testFile }, "test-cursor-page-1", false));
        Mock<IDownloadResponse<FileMetadata>> downloadResponse = new();
        downloadResponse.Setup(x => x.GetContentAsStreamAsync()).ReturnsAsync(memoryStream);
        downloadResponse.Setup(x => x.Response).Returns(testFile);
        _dropboxClientProxy.Setup(x => x.DownloadAsync("/photos/test.jpg")).ReturnsAsync(downloadResponse.Object);

        {
            using var serviceScope = _factory!.Services.CreateScope();
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

    [Fact]
    public async Task Should_sync_photo_from_sub_directory()
    {
        {
            using var serviceScope = _factory!.Services.CreateScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
            var album = await context.AlbumSources!.SingleAsync(x => x.Folder == "/photos");
            album.RecurseSubFolders = true;
            await context.SaveChangesAsync();
        }

        using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 15, 10);
        await using MemoryStream memoryStream = new();
        await img.WriteAsync(memoryStream, MagickFormat.Jpeg);
        memoryStream.Position = 0;
        FileMetadata testFile = new("test HDR.jpg", "photo-1", DateTime.UtcNow, DateTime.UtcNow, "000000001", 100, pathLower: "/photos/sub-dir/test hdr.jpg");
        _dropboxClientProxy.Setup(x => x.ListFolderAsync("/photos", true)).ReturnsAsync(new ListFolderResult(new[] { testFile }, "test-cursor-page-1", false));
        Mock<IDownloadResponse<FileMetadata>> downloadResponse = new();
        downloadResponse.Setup(x => x.GetContentAsStreamAsync()).ReturnsAsync(memoryStream);
        downloadResponse.Setup(x => x.Response).Returns(testFile);
        _dropboxClientProxy.Setup(x => x.DownloadAsync("/photos/sub-dir/test HDR.jpg")).ReturnsAsync(downloadResponse.Object);

        {
            using var serviceScope = _factory!.Services.CreateScope();
            var albumSyncService = serviceScope.ServiceProvider.GetRequiredService<IAlbumSyncService>();
            await albumSyncService.SyncAllAsync(CancellationToken.None);
        }

        {
            using var serviceScope = _factory.Services.CreateScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
            (await context.Photos!.CountAsync()).Should().Be(1);
            var newPhoto = await context.Photos!.FirstAsync();
            newPhoto.AlbumSourceId.Should().Be(_albumSource!.AlbumSourceId);
            newPhoto.Filename.Should().Be("test HDR.jpg");
            newPhoto.RelativePath.Should().Be("/sub-dir");
            newPhoto.Width.Should().Be(15);
            newPhoto.Height.Should().Be(10);
            newPhoto.DateTaken.Should().BeNull();
            newPhoto.DeletedDateTime.Should().BeNull();
        }
    }

    [Fact]
    public async Task Should_remove_old_photo_and_add_new()
    {
        {
            // initial image
            using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 15, 10);
            MemoryStream memoryStream = new();
            await img.WriteAsync(memoryStream, MagickFormat.Jpeg);
            memoryStream.Position = 0;
            FileMetadata test1File = new("test1.jpg", "photo-1", DateTime.UtcNow, DateTime.UtcNow, "000000001", 100);
            _dropboxClientProxy.Setup(x => x.ListFolderAsync("/photos", false)).ReturnsAsync(new ListFolderResult(new[] { test1File }, "test-cursor-page-1", false));
            Mock<IDownloadResponse<FileMetadata>> downloadResponse = new();
            downloadResponse.Setup(x => x.GetContentAsStreamAsync()).ReturnsAsync(memoryStream);
            downloadResponse.Setup(x => x.Response).Returns(test1File);
            _dropboxClientProxy.Setup(x => x.DownloadAsync("/photos/test1.jpg")).ReturnsAsync(downloadResponse.Object);
        }

        {
            using var serviceScope = _factory!.Services.CreateScope();
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
            FileMetadata test2File;
            FileMetadata test3File;
            {
                using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 25, 20);
                MemoryStream memoryStreamTest = new();
                await img.WriteAsync(memoryStreamTest, MagickFormat.Jpeg);
                memoryStreamTest.Position = 0;
                test2File = new("test2.jpg", "photo-2", DateTime.UtcNow, DateTime.UtcNow, "000000001", 100);

                Mock<IDownloadResponse<FileMetadata>> downloadResponse = new();
                downloadResponse.Setup(x => x.GetContentAsStreamAsync()).ReturnsAsync(memoryStreamTest);
                downloadResponse.Setup(x => x.Response).Returns(test2File);
                _dropboxClientProxy.Setup(x => x.DownloadAsync("/photos/test2.jpg")).ReturnsAsync(downloadResponse.Object);
            }
            {
                using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 35, 30);
                MemoryStream memoryStreamTest = new();
                await img.WriteAsync(memoryStreamTest, MagickFormat.Jpeg);
                memoryStreamTest.Position = 0;
                test3File = new("test3.jpg", "photo-3", DateTime.UtcNow, DateTime.UtcNow, "000000001", 100);

                Mock<IDownloadResponse<FileMetadata>> downloadResponse = new();
                downloadResponse.Setup(x => x.GetContentAsStreamAsync()).ReturnsAsync(memoryStreamTest);
                downloadResponse.Setup(x => x.Response).Returns(test3File);
                _dropboxClientProxy.Setup(x => x.DownloadAsync("/photos/test3.jpg")).ReturnsAsync(downloadResponse.Object);
            }

            // let's check the paged / cursor folder listing works ok
            _dropboxClientProxy.Setup(x => x.ListFolderAsync("/photos", false)).ReturnsAsync(new ListFolderResult(new[] { test2File }, "test-cursor-page-1", true));
            _dropboxClientProxy.Setup(x => x.ListFolderContinueAsync("test-cursor-page-1")).ReturnsAsync(new ListFolderResult(new[] { test3File }, "test-cursor-page-2", false));
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

    public Task DisposeAsync()
    {
        Console.WriteLine($"Cleaning up photo source dir: [{_dropboxTemporaryFolder}]");
        if (!string.IsNullOrEmpty(_dropboxTemporaryFolder) && Directory.Exists(_dropboxTemporaryFolder))
            Directory.Delete(_dropboxTemporaryFolder, true);

        _factory?.Dispose();
        return Task.CompletedTask;
    }
}