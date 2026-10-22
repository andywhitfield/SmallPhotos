using System.Net;
using FluentAssertions;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Tests;

[TestClass]
public class MapTest
{
    private readonly IntegrationTestWebApplicationFactory _factory = new();
    private string? _albumSourceFolder;
    private Photo? _photo1;
    private Photo? _photo2;
    private Photo? _photo3;
    private Photo? _photo4;
    private Photo? _photo5;
    private Photo? _photo6;

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

        _photo1 = await CreatePhotoAsync(context, album.Entity, "photo1.jpg", new(2026, 9, 1, 9, 30, 0, DateTimeKind.Utc), 10, 100);
        _photo2 = await CreatePhotoAsync(context, album.Entity, "photo2.jpg", new(2026, 9, 2, 9, 30, 0, DateTimeKind.Utc), null, 200);
        _photo3 = await CreatePhotoAsync(context, album.Entity, "photo3.jpg", new(2026, 9, 3, 9, 30, 0, DateTimeKind.Utc), 30, 300);
        _photo4 = await CreatePhotoAsync(context, album.Entity, "photo4.jpg", new(2026, 9, 4, 9, 30, 0, DateTimeKind.Utc), 40, null);
        _photo5 = await CreatePhotoAsync(context, album.Entity, "photo5.jpg", new(2026, 9, 5, 9, 30, 0, DateTimeKind.Utc), 50, 500);
        _photo6 = await CreatePhotoAsync(context, album.Entity, "photo6.jpg", new(2026, 9, 6, 9, 30, 0, DateTimeKind.Utc), null, null);

        await context.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Should_map_photos_with_geo_coordinates()
    {
        using var client = _factory.CreateAuthenticatedClient();
        using var homeResponse = await client.GetAsync("/");
        homeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var homePage = await homeResponse.Content.ReadAsStringAsync();
        using var mapResponse = await client.PostAsync("/map", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "view", SelectedView.All.ToString() },
            { "photoids", string.Join(',', [_photo1!.PhotoId, _photo2!.PhotoId, _photo3!.PhotoId, _photo4!.PhotoId, _photo5!.PhotoId, _photo6!.PhotoId]) },
            { "__RequestVerificationToken", IntegrationTestWebApplicationFactory.GetFormValidationToken(homePage, "/map") }
        }));
        mapResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var mapPage = await mapResponse.Content.ReadAsStringAsync();

        mapPage.Should().Contain($""" id="map" """);
        mapPage.Should().Contain($"L.map('map')");

        mapPage.Should().Contain("mapPoints.push([10, 100])");
        mapPage.Should().Contain($""" href="/gallery/{_photo1.PhotoId}/photo1.jpg" """);
        mapPage.Should().Contain("Taken: 09:30 on Tuesday 01 September 2026");

        mapPage.Should().NotContain($""" href="/gallery/{_photo2.PhotoId}/photo2.jpg" """);

        mapPage.Should().Contain("mapPoints.push([30, 300])");
        mapPage.Should().Contain($""" href="/gallery/{_photo3.PhotoId}/photo3.jpg" """);
        mapPage.Should().Contain("Taken: 09:30 on Thursday 03 September 2026");

        mapPage.Should().NotContain($""" href="/gallery/{_photo4.PhotoId}/photo4.jpg" """);

        mapPage.Should().Contain("mapPoints.push([50, 500])");
        mapPage.Should().Contain($""" href="/gallery/{_photo5.PhotoId}/photo5.jpg" """);
        mapPage.Should().Contain("Taken: 09:30 on Saturday 05 September 2026");

        mapPage.Should().NotContain($""" href="/gallery/{_photo6.PhotoId}/photo6.jpg" """);
    }

    [TestMethod]
    public async Task Given_no_photos_with_geo_coordinates_Should_not_show_map()
    {
        using var client = _factory.CreateAuthenticatedClient();
        using var homeResponse = await client.GetAsync("/");
        homeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var homePage = await homeResponse.Content.ReadAsStringAsync();
        using var mapResponse = await client.PostAsync("/map", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "view", SelectedView.All.ToString() },
            { "photoids", string.Join(',', [_photo2!.PhotoId, _photo4!.PhotoId, _photo6!.PhotoId]) },
            { "__RequestVerificationToken", IntegrationTestWebApplicationFactory.GetFormValidationToken(homePage, "/map") }
        }));
        mapResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var mapPage = await mapResponse.Content.ReadAsStringAsync();

        mapPage.Should().NotContain($""" id="map" """);
        mapPage.Should().NotContain($"L.map('map')");
        mapPage.Should().NotContain($""" href="/gallery/{_photo2.PhotoId}/photo2.jpg" """);
        mapPage.Should().NotContain($""" href="/gallery/{_photo4.PhotoId}/photo4.jpg" """);
        mapPage.Should().NotContain($""" href="/gallery/{_photo6.PhotoId}/photo6.jpg" """);
    }

    private async Task<Photo> CreatePhotoAsync(SqliteDataContext context, AlbumSource album, string filename, DateTime dateTaken, double? lat, double? lng)
    {
        using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 1, 1);
        await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", filename), MagickFormat.Jpeg);
        return context.Photos!.Add(new() { AlbumSource = album, CreatedDateTime = dateTaken, FileCreationDateTime = dateTaken, DateTaken = dateTaken, Filename = filename, Height = 1, Width = 1, Latitude = lat, Longitude = lng }).Entity;
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