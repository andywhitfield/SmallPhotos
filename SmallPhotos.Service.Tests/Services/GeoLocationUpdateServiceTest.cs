using System.Net;
using System.Text.Json;
using System.Web;
using BigDataCloud;
using BigDataCloud.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using SmallPhotos.Data;
using SmallPhotos.Service.BackgroundServices;
using SmallPhotos.Service.Services;

namespace SmallPhotos.Service.Tests.Services;

[TestClass]
public class GeoLocationUpdateServiceTest
{
    private readonly IntegrationTestWebApplicationFactory _factory;
    private readonly Dictionary<(int Lat, int Lon), (string Locality, string PrincipalSubdivision, string City, string CountryName)> _geoLookup = [];
    private int _rateLimit = int.MaxValue;
    private TimeSpan _rateLimitWindow = TimeSpan.FromSeconds(1);

    public GeoLocationUpdateServiceTest() => _factory = new(ConfigureTestServices);

    [TestInitialize]
    public async Task InitializeAsync()
    {
        using var serviceScope = _factory.Services.CreateScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();

        var userAccount = context.UserAccounts.Add(new() { Email = "test-user-1" });
        context.AlbumSources.Add(new() { UserAccount = userAccount.Entity, Folder = "TEST" });
        await context.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Should_update_only_photos_needing_lookup()
    {
        await AddPhotoAsync("img1.jpg", false, -1, 51);
        await AddPhotoAsync("img2.jpg", true, -2, 52);
        await AddPhotoAsync("img3.jpg", false, -3, 53);

        _geoLookup.Add((-1, 51), ("place 1", "principal 1", "city 1", "country 1"));
        _geoLookup.Add((-2, 52), ("place 2", "principal 2", "city 2", "country 2"));
        _geoLookup.Add((-3, 53), ("place 3", "principal 3", "city 3", "country 3"));

        {
            using var serviceScope = _factory.Services.CreateScope();
            var updateService = serviceScope.ServiceProvider.GetRequiredService<IGeoLocationUpdateService>();
            await updateService.UpdateAsync(CancellationToken.None);
        }

        {
            using var serviceScope = _factory.Services.CreateScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();

            var photo = await context.Photos.SingleAsync(p => p.Filename == "img1.jpg");
            Assert.AreEqual("place 1", photo.GeoLocality);
            Assert.AreEqual("principal 1", photo.GeoPrincipalSubdivision);
            Assert.AreEqual("city 1", photo.GeoCity);
            Assert.AreEqual("country 1", photo.GeoCountryName);
            Assert.IsTrue(photo.IsGeoLookupCompleted);

            photo = await context.Photos.SingleAsync(p => p.Filename == "img2.jpg");
            Assert.IsNull(photo.GeoLocality);
            Assert.IsNull(photo.GeoPrincipalSubdivision);
            Assert.IsNull(photo.GeoCity);
            Assert.IsNull(photo.GeoCountryName);

            photo = await context.Photos.SingleAsync(p => p.Filename == "img3.jpg");
            Assert.AreEqual("place 3", photo.GeoLocality);
            Assert.AreEqual("principal 3", photo.GeoPrincipalSubdivision);
            Assert.AreEqual("city 3", photo.GeoCity);
            Assert.AreEqual("country 3", photo.GeoCountryName);
            Assert.IsTrue(photo.IsGeoLookupCompleted);
        }
    }

    [TestMethod]
    public async Task Should_throttle_updates_to_external_api()
    {
        await AddPhotoAsync("img1.jpg", false, -1, 51);
        await AddPhotoAsync("img2.jpg", false, -2, 52);
        await AddPhotoAsync("img3.jpg", false, -3, 53);

        _geoLookup.Add((-1, 51), ("place 1", "principal 1", "city 1", "country 1"));
        _geoLookup.Add((-2, 52), ("place 2", "principal 2", "city 2", "country 2"));
        _geoLookup.Add((-3, 53), ("place 3", "principal 3", "city 3", "country 3"));

        _rateLimit = 1;
        _rateLimitWindow = TimeSpan.FromSeconds(2);

        DateTime startAt;
        DateTime completedAt;
        
        {
            using var serviceScope = _factory.Services.CreateScope();
            var updateService = serviceScope.ServiceProvider.GetRequiredService<IGeoLocationUpdateService>();
            startAt = DateTime.UtcNow;
            await updateService.UpdateAsync(CancellationToken.None);
            completedAt = DateTime.UtcNow;
        }

        {
            using var serviceScope = _factory.Services.CreateScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();

            var photo = await context.Photos.SingleAsync(p => p.Filename == "img1.jpg");
            Assert.AreEqual("place 1", photo.GeoLocality);
            Assert.AreEqual("principal 1", photo.GeoPrincipalSubdivision);
            Assert.AreEqual("city 1", photo.GeoCity);
            Assert.AreEqual("country 1", photo.GeoCountryName);
            Assert.IsTrue(photo.IsGeoLookupCompleted);

            photo = await context.Photos.SingleAsync(p => p.Filename == "img2.jpg");
            Assert.AreEqual("place 2", photo.GeoLocality);
            Assert.AreEqual("principal 2", photo.GeoPrincipalSubdivision);
            Assert.AreEqual("city 2", photo.GeoCity);
            Assert.AreEqual("country 2", photo.GeoCountryName);
            Assert.IsTrue(photo.IsGeoLookupCompleted);

            photo = await context.Photos.SingleAsync(p => p.Filename == "img3.jpg");
            Assert.AreEqual("place 3", photo.GeoLocality);
            Assert.AreEqual("principal 3", photo.GeoPrincipalSubdivision);
            Assert.AreEqual("city 3", photo.GeoCity);
            Assert.AreEqual("country 3", photo.GeoCountryName);
            Assert.IsTrue(photo.IsGeoLookupCompleted);

            Assert.IsGreaterThanOrEqualTo(TimeSpan.FromSeconds(4), completedAt - startAt,
                "There should have been a 2s throttling between each external api call, so should have taken at least 4s.");
        }
    }

    [TestCleanup]
    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private async Task AddPhotoAsync(string filename, bool isGeoLookupCompleted, int? lat, int? lon)
    {
        using var serviceScope = _factory.Services.CreateScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        context.Photos.Add(new()
        {
            AlbumSource = await context.AlbumSources.SingleAsync(),
            Filename = filename,
            IsGeoLookupCompleted = isGeoLookupCompleted,
            Latitude = lat,
            Longitude = lon
        });
        await context.SaveChangesAsync();
    }

    private IServiceCollection ConfigureTestServices(IServiceCollection services)
    {
        Mock<HttpMessageHandler> mockHttpMessageHandler = new();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                var queryParams = HttpUtility.ParseQueryString(request.RequestUri?.Query ?? "");
                var geoLookupResult = _geoLookup.TryGetValue(
                    (int.TryParse(queryParams.Get("latitude"), out var lat) ? lat : 0,
                    int.TryParse(queryParams.Get("longitude"), out var lon) ? lon : 0),
                    out var result) ? result : default;

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(new ReverseGeocodeResponse
                    {
                        Locality = geoLookupResult.Locality,
                        PrincipalSubdivision = geoLookupResult.PrincipalSubdivision,
                        City = geoLookupResult.City,
                        CountryName = geoLookupResult.CountryName
                    }))
                };
            });

        BigDataCloudClient bigDataCloudClient = new("ignored", new HttpClient(mockHttpMessageHandler.Object) { BaseAddress = new("https://dummay.url/data/") });
        services.Replace(ServiceDescriptor.Scoped(_ => bigDataCloudClient));

        services.Replace(ServiceDescriptor.Scoped(_ =>
        {
            Mock<IOptionsSnapshot<GeoLocationServiceOptions>> options = new();
            options.Setup(o => o.Value).Returns(new GeoLocationServiceOptions { RateLimit = _rateLimit, RateLimitWindow = _rateLimitWindow });
            return options.Object;
        }));

        return services;
    }
}
