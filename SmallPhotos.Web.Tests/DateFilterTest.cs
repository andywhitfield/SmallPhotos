using System.Net;
using FluentAssertions;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using SmallPhotos.Data;
using SmallPhotos.Model;

namespace SmallPhotos.Web.Tests;

[TestClass]
public class DateFilterTest
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

        var userAccount = context.UserAccounts!.Add(new() { Email = "test-user-1", GalleryImagePageSize = 2 });
        var album = context.AlbumSources!.Add(new() { CreatedDateTime = DateTime.UtcNow, Folder = _albumSourceFolder, UserAccount = userAccount.Entity });

        await CreatePhotoAsync(context, album.Entity, "photo1.jpg", new(2026, 9, 2, 9, 30, 0, DateTimeKind.Utc), tags: "tag1 tag2");
        await CreatePhotoAsync(context, album.Entity, "photo2.jpg", new(2026, 9, 3, 9, 30, 0, DateTimeKind.Utc), tags: "tag1");
        await CreatePhotoAsync(context, album.Entity, "photo3.jpg", new(2026, 9, 3, 9, 45, 0, DateTimeKind.Utc), tags: "tag1");
        await CreatePhotoAsync(context, album.Entity, "photo4.jpg", new(2026, 9, 4, 9, 30, 0, DateTimeKind.Utc), isStarred: true);
        await CreatePhotoAsync(context, album.Entity, "photo5.jpg", new(2026, 9, 5, 9, 30, 0, DateTimeKind.Utc));
        await CreatePhotoAsync(context, album.Entity, "photo6.jpg", new(2026, 9, 5, 13, 30, 0, DateTimeKind.Utc), tags: "tag2 tag3");

        await context.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Home_page_should_show_date_filter()
    {
        using var client = _factory.CreateAuthenticatedClient();
        using var response = await client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("""<a id="filter-date-button" title="Filter by date">""");
        responseContent.Should().Contain("data-mindate=\"2026-09-02\"", Exactly.Once());
        responseContent.Should().Contain("data-maxdate=\"2026-09-05\"", Exactly.Once());
        responseContent.Should().Contain("data-filter-applied=\"\"", Exactly.Once());
        responseContent.Should().Contain("""<input type="text" name="fromdate" readonly value="02 Sep 2026" class="filter-date" />""", Exactly.Once());
        responseContent.Should().Contain("""<input type="text" name="todate" readonly value="05 Sep 2026" class="filter-date" />""", Exactly.Once());
        // there should be 6 photos in total, 2 per page, so 3 pages in total
        responseContent.Should().Contain("<img src=\"/photo/thumbnail", Exactly.Times(2));
        responseContent.Should().Contain("""<a class="active">1</a>""", Exactly.Times(2));
        responseContent.Should().Contain("Go to photo page 2", Exactly.Times(2));
        responseContent.Should().Contain("Go to photo page 3", Exactly.Times(2));
        responseContent.Should().NotContain("Go to photo page 4");
    }

    [TestMethod]
    public async Task Should_only_show_photos_within_specified_date_filter_range()
    {
        using var client = _factory.CreateAuthenticatedClient();
        using var response = await client.GetAsync("/?fromDate=2026-09-02&toDate=2026-09-04");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("""<a id="filter-date-button" title="Filter by date">""");
        responseContent.Should().Contain("data-mindate=\"2026-09-02\"", Exactly.Once());
        responseContent.Should().Contain("data-maxdate=\"2026-09-05\"", Exactly.Once());
        responseContent.Should().Contain("data-filter-applied=\"true\"", Exactly.Once());
        responseContent.Should().Contain("""<input type="text" name="fromdate" readonly value="02 Sep 2026" class="filter-date" />""", Exactly.Once());
        responseContent.Should().Contain("""<input type="text" name="todate" readonly value="04 Sep 2026" class="filter-date" />""", Exactly.Once());
        // there should be 4 photos in total, 2 per page, so 2 pages in total
        responseContent.Should().Contain("""<img src="/photo/thumbnail/Small/3/photo3.jpg""", Exactly.Once());
        responseContent.Should().Contain("""<img src="/photo/thumbnail/Small/4/photo4.jpg""", Exactly.Once());
        responseContent.Should().Contain("""<a class="active">1</a>""", Exactly.Times(2));
        responseContent.Should().Contain("""<a title="Go to photo page 2" href="/?pageNumber=2&fromDate=2026-09-02&toDate=2026-09-04">2</a>""", Exactly.Times(2));
        responseContent.Should().NotContain("Go to photo page 3");
    }

    [TestMethod]
    public async Task Should_show_photos_on_page_2_that_were_taken_within_specified_date_filter_range()
    {
        using var client = _factory.CreateAuthenticatedClient();
        using var response = await client.GetAsync("/?pageNumber=2&fromDate=2026-09-02&toDate=2026-09-04");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("""<a id="filter-date-button" title="Filter by date">""");
        responseContent.Should().Contain("data-mindate=\"2026-09-02\"", Exactly.Once());
        responseContent.Should().Contain("data-maxdate=\"2026-09-05\"", Exactly.Once());
        responseContent.Should().Contain("data-filter-applied=\"true\"", Exactly.Once());
        responseContent.Should().Contain("""<input type="text" name="fromdate" readonly value="02 Sep 2026" class="filter-date" />""", Exactly.Once());
        responseContent.Should().Contain("""<input type="text" name="todate" readonly value="04 Sep 2026" class="filter-date" />""", Exactly.Once());
        // there should be 4 photos in total, 2 per page, so 2 pages in total
        responseContent.Should().Contain("""<img src="/photo/thumbnail/Small/1/photo1.jpg""", Exactly.Once());
        responseContent.Should().Contain("""<img src="/photo/thumbnail/Small/2/photo2.jpg""", Exactly.Once());
        responseContent.Should().Contain("""<a title="Go to photo page 1" href="/?pageNumber=1&fromDate=2026-09-02&toDate=2026-09-04">1</a>""", Exactly.Times(2));
        responseContent.Should().Contain("""<a class="active">2</a>""", Exactly.Times(2));
        responseContent.Should().NotContain("Go to photo page 3");
    }

    [TestMethod]
    public async Task Should_show_photo_that_is_starred_without_any_date_filtering()
    {
        using var client = _factory.CreateAuthenticatedClient();
        using var response = await client.GetAsync("/starred");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotContain("""<a id="filter-date-button" title="Filter by date">""");
        // there should be 1 photo
        responseContent.Should().Contain("""<img src="/photo/thumbnail""", Exactly.Once());
        responseContent.Should().Contain("""<img src="/photo/thumbnail/Small/4/photo4.jpg""", Exactly.Once());
    }

    private async Task CreatePhotoAsync(SqliteDataContext context, AlbumSource album, string filename, DateTime dateTaken, string? tags = null, bool? isStarred = false)
    {
        using MagickImage img = new(new MagickColor(ushort.MaxValue, 0, 0), 1, 1);
        await img.WriteAsync(Path.Combine(_albumSourceFolder ?? "", filename), MagickFormat.Jpeg);
        var photo = context.Photos!.Add(new() { AlbumSource = album, CreatedDateTime = dateTaken, FileCreationDateTime = dateTaken, DateTaken = dateTaken, Filename = filename, Height = 1, Width = 1 });
        if (tags != null)
            context.PhotoTags!.AddRange(tags.Split(' ').Select(t => new PhotoTag { Photo = photo.Entity, Tag = t, UserAccount = album.UserAccount }));
        if (isStarred ?? false)
            context.StarredPhotos!.Add(new() { Photo = photo.Entity, UserAccount = album.UserAccount });
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