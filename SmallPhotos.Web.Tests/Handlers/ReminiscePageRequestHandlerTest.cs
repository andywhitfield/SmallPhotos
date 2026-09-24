using System.Globalization;
using System.Security.Claims;
using ImageMagick;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Tests.Handlers;

[TestClass]
public class ReminiscePageRequestHandlerTest
{
    private readonly FakeTimeProvider _fakeTimeProvider = new();
    private readonly IntegrationTestWebApplicationFactory _factory = new();
    private string? _albumSourceFolder;

    [TestMethod]
    [DataRow("2026-09-01 10:20:00", "1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 23, 24, 25, 26, 27", "17, 18, 19, 20, 21, 22")]
    [DataRow("2026-09-16 11:30:00", "1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27", "")]
    [DataRow("2026-09-22 12:40:00", "6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22", "1, 2, 3, 4, 5, 23, 24, 25, 26, 27")]
    [DataRow("2026-08-16 13:50:00", "23, 26, 27", "1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 24, 25")]
    [DataRow("2026-08-15 14:50:00", "26", "1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 27")]
    [DataRow("2026-08-14 15:50:00", "", "1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27")]
    [DataRow("2026-10-06 16:30:00", "20, 21, 22", "1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 23, 24, 25, 26, 27")]
    [DataRow("2026-10-07 17:30:00", "21, 22", "1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 23, 24, 25, 26, 27")]
    [DataRow("2026-10-08 18:30:00", "22", "1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 23, 24, 25, 26, 27")]
    [DataRow("2026-10-09 19:30:00", "", "1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27")]
    public async Task Should_get_photos_from_previous_years(string now, string photosIncluded, string photosNotIncluded)
    {
        _fakeTimeProvider.SetUtcNow(DateTime.ParseExact(now, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

        await using var serviceScope = _factory.Services.CreateAsyncScope();
        var mediator = serviceScope.ServiceProvider.GetRequiredService<IMediator>();

        ClaimsIdentity claimsIdentity = new((List<Claim>)[new Claim(ClaimTypes.Name, "test-user-1")], CookieAuthenticationDefaults.AuthenticationScheme);
        var response = await mediator.Send(new ReminiscePageRequest(new(claimsIdentity)));
        Assert.IsTrue(response.IsUserValid);

        var photosIncludedList = photosIncluded.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(int.Parse);
        Assert.HasCount(Math.Min(photosIncludedList.Count(), 6), response.Photos);

        AssertPhotosIncluded(response.Photos, photosIncludedList);
        AssertPhotosNotIncluded(response.Photos, photosNotIncluded.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(int.Parse));
    }

    private static void AssertPhotosNotIncluded(IEnumerable<PhotoModel> photos, IEnumerable<int> filenames)
    {
        foreach (var photo in photos)
            Assert.DoesNotContain(int.Parse(Path.GetFileNameWithoutExtension(photo.Filename)), filenames);
    }

    private static void AssertPhotosIncluded(IEnumerable<PhotoModel> photos, IEnumerable<int> filenames)
    {
        foreach (var photo in photos)
            Assert.Contains(int.Parse(Path.GetFileNameWithoutExtension(photo.Filename)), filenames);
    }

    [TestInitialize]
    public async Task SetupAsync()
    {
        _factory.TestTimeProvider = _fakeTimeProvider;

        _albumSourceFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Console.WriteLine($"Using photo source dir: [{_albumSourceFolder}]");
        Directory.CreateDirectory(_albumSourceFolder);

        await using var serviceScope = _factory.Services.CreateAsyncScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var userAccount = context.UserAccounts!.Add(new() { Email = "test-user-1" });
        context.AlbumSources!.Add(new() { CreatedDateTime = DateTime.UtcNow, Folder = _albumSourceFolder, UserAccount = userAccount.Entity });
        await context.SaveChangesAsync();

        await CreatePhotoAsync("1.jpg", new(2025, 9, 1));
        await CreatePhotoAsync("2.jpg", new(2025, 9, 2));
        await CreatePhotoAsync("3.jpg", new(2025, 9, 3));
        await CreatePhotoAsync("4.jpg", new(2025, 9, 4));
        await CreatePhotoAsync("5.jpg", new(2025, 9, 5));
        await CreatePhotoAsync("6.jpg", new(2025, 9, 6));
        await CreatePhotoAsync("7.jpg", new(2025, 9, 7));
        await CreatePhotoAsync("8.jpg", new(2025, 9, 8));
        await CreatePhotoAsync("9.jpg", new(2025, 9, 9));
        await CreatePhotoAsync("10.jpg", new(2025, 9, 10));
        await CreatePhotoAsync("11.jpg", new(2025, 9, 11));
        await CreatePhotoAsync("12.jpg", new(2025, 9, 12));
        await CreatePhotoAsync("13.jpg", new(2025, 9, 13));
        await CreatePhotoAsync("14.jpg", new(2025, 9, 14));
        await CreatePhotoAsync("15.jpg", new(2025, 9, 15));
        await CreatePhotoAsync("16.jpg", new(2025, 9, 16));
        await CreatePhotoAsync("17.jpg", new(2025, 9, 17));
        await CreatePhotoAsync("18.jpg", new(2025, 9, 18));
        await CreatePhotoAsync("19.jpg", new(2025, 9, 19));
        await CreatePhotoAsync("20.jpg", new(2025, 9, 20));
        await CreatePhotoAsync("21.jpg", new(2025, 9, 21));
        await CreatePhotoAsync("22.jpg", new(2025, 9, 22));

        await CreatePhotoAsync("23.jpg", new(2024, 9, 1));
        await CreatePhotoAsync("24.jpg", new(2024, 9, 2));
        await CreatePhotoAsync("25.jpg", new(2024, 9, 3));

        await CreatePhotoAsync("26.jpg", new(2023, 9, 1));
        await CreatePhotoAsync("27.jpg", new(2023, 9, 2));
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

    private async Task CreatePhotoAsync(string filename, DateTime dateTaken)
    {
        using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 15, 10);
        await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", "1.jpg"), MagickFormat.Jpeg);

        await using var serviceScope = _factory.Services.CreateAsyncScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var album = await context.AlbumSources!.SingleAsync();
        context.Photos!.Add(new() { AlbumSource = album, CreatedDateTime = dateTaken, FileCreationDateTime = dateTaken, DateTaken = dateTaken, Filename = filename, Height = 10, Width = 15 });
        await context.SaveChangesAsync();
    }
}
