using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmallPhotos.Data;

namespace SmallPhotos.Web.Tests.Controllers;

[TestClass]
public class FeedControllerTests
{
    private readonly IntegrationTestWebApplicationFactory _factory = new();

    [TestInitialize]
    public async Task InitializeAsync()
    {
        await using var serviceScope = _factory.Services.CreateAsyncScope();

        var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var userAccount = context.UserAccounts!.Add(new() { Email = "test-user-1" });
        await context.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Invalid_feed_identifier_returns_notfound()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/feed/test-1234");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Valid_feed_identifier_returns_empty_rss()
    {
        await ConfigureDbContextAsync(async context =>
        {
            var testUser = await context.UserAccounts.SingleAsync();
            context.UserFeeds.Add(new() { UserAccount = testUser, UserFeedIdentifier = "test-1234" });
        });

        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/feed/test-1234");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var feedContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("feed", feedContent);
        Assert.Contains("title", feedContent);
        Assert.DoesNotContain("Small:Photos Reminisce photos", feedContent);
        Assert.DoesNotContain("Small:Photos Recently added photos", feedContent);
        Assert.DoesNotContain("Small:Photos Recently added and Reminisce photos", feedContent);
        Assert.DoesNotContain("entry", feedContent);
    }

    [TestMethod]
    public async Task Should_contain_only_reminisce_photos_in_feed()
    {
        await ConfigureDbContextAsync(async context =>
        {
            var testUser = await context.UserAccounts.SingleAsync();
            context.UserFeeds.Add(new() { UserAccount = testUser, UserFeedIdentifier = "test-1234", LastUpdateDateTime = DateTime.UtcNow.AddDays(-1) });

            var album = context.AlbumSources.Add(new() { CreatedDateTime = DateTime.UtcNow, Folder = "invalid", UserAccount = testUser });
            context.Photos.Add(new() { AlbumSource = album.Entity, DateTaken = DateTime.UtcNow.AddDays(-2), CreatedDateTime = DateTime.UtcNow.AddDays(-2), FileCreationDateTime = DateTime.UtcNow, Filename = "photo1.jpg", Height = 10, Width = 15 });
            context.Photos.Add(new() { AlbumSource = album.Entity, DateTaken = DateTime.UtcNow.AddYears(-1), CreatedDateTime = DateTime.UtcNow.AddYears(-1), FileCreationDateTime = DateTime.UtcNow, Filename = "photo2.jpg", Height = 11, Width = 16 });
            context.Photos.Add(new() { AlbumSource = album.Entity, DateTaken = DateTime.UtcNow.AddYears(-2), CreatedDateTime = DateTime.UtcNow.AddYears(-2), FileCreationDateTime = DateTime.UtcNow, Filename = "photo3.jpg", Height = 12, Width = 17 });
            context.Photos.Add(new() { AlbumSource = album.Entity, DateTaken = DateTime.UtcNow.AddYears(-3), CreatedDateTime = DateTime.UtcNow.AddYears(-3), FileCreationDateTime = DateTime.UtcNow, Filename = "photo4.jpg", Height = 13, Width = 18 });
        });

        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/feed/test-1234");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var feedContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("feed", feedContent);
        Assert.Contains("title", feedContent);
        Assert.Contains("Small:Photos Reminisce photos", feedContent);
        Assert.DoesNotContain("Small:Photos Recently added photos", feedContent);
        Assert.DoesNotContain("Small:Photos Recently added and Reminisce photos", feedContent);

        Assert.DoesNotContain("photo1.jpg", feedContent);
        Assert.Contains("photo2.jpg", feedContent);
        Assert.Contains("photo3.jpg", feedContent);
        Assert.Contains("photo4.jpg", feedContent);
    }

    [TestMethod]
    public async Task Should_contain_recently_added_and_reminisce_photos_in_feed()
    {
        await ConfigureDbContextAsync(async context =>
        {
            var testUser = await context.UserAccounts.SingleAsync();
            context.UserFeeds.Add(new() { UserAccount = testUser, UserFeedIdentifier = "test-1234", LastUpdateDateTime = DateTime.UtcNow.AddDays(-2) });

            var album = context.AlbumSources.Add(new() { CreatedDateTime = DateTime.UtcNow, Folder = "invalid", UserAccount = testUser });
            context.Photos.Add(new() { AlbumSource = album.Entity, DateTaken = DateTime.UtcNow.AddDays(-1), CreatedDateTime = DateTime.UtcNow.AddDays(-2), FileCreationDateTime = DateTime.UtcNow, Filename = "photo1.jpg", Height = 10, Width = 15 });
            context.Photos.Add(new() { AlbumSource = album.Entity, DateTaken = DateTime.UtcNow.AddYears(-1), CreatedDateTime = DateTime.UtcNow.AddYears(-1), FileCreationDateTime = DateTime.UtcNow, Filename = "photo2.jpg", Height = 11, Width = 16 });
            context.Photos.Add(new() { AlbumSource = album.Entity, DateTaken = DateTime.UtcNow.AddYears(-2), CreatedDateTime = DateTime.UtcNow.AddYears(-2), FileCreationDateTime = DateTime.UtcNow, Filename = "photo3.jpg", Height = 12, Width = 17 });
            context.Photos.Add(new() { AlbumSource = album.Entity, DateTaken = DateTime.UtcNow.AddYears(-3), CreatedDateTime = DateTime.UtcNow.AddYears(-3), FileCreationDateTime = DateTime.UtcNow, Filename = "photo4.jpg", Height = 13, Width = 18 });
            context.Photos.Add(new() { AlbumSource = album.Entity, DateTaken = DateTime.UtcNow.AddYears(-3).AddDays(20), CreatedDateTime = DateTime.UtcNow.AddYears(-3).AddDays(20), FileCreationDateTime = DateTime.UtcNow, Filename = "photo5.jpg", Height = 14, Width = 19 });
            context.Photos.Add(new() { AlbumSource = album.Entity, DateTaken = DateTime.UtcNow.AddYears(-3).AddDays(-20), CreatedDateTime = DateTime.UtcNow.AddYears(-3).AddDays(-20), FileCreationDateTime = DateTime.UtcNow, Filename = "photo6.jpg", Height = 15, Width = 20 });
        });

        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/api/feed/test-1234");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var feedContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("feed", feedContent);
        Assert.Contains("title", feedContent);
        Assert.DoesNotContain("Small:Photos Reminisce photos", feedContent);
        Assert.DoesNotContain("Small:Photos Recently added photos", feedContent);
        Assert.Contains("Small:Photos Recently added and Reminisce photos", feedContent);

        Assert.Contains("photo1.jpg", feedContent);
        Assert.Contains("photo2.jpg", feedContent);
        Assert.Contains("photo3.jpg", feedContent);
        Assert.Contains("photo4.jpg", feedContent);
        Assert.DoesNotContain("photo5.jpg", feedContent);
        Assert.DoesNotContain("photo6.jpg", feedContent);
    }

    private async Task ConfigureDbContextAsync(Func<SqliteDataContext, Task> configure)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        await configure(dbContext);
        await dbContext.SaveChangesAsync();
    }
}