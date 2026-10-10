using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using SmallPhotos.Data;
using SmallPhotos.Feed;
using SmallPhotos.Model;

namespace SmallPhotos.Tests;

[TestClass]
public class AtomFeedGeneratorTest
{
    [TestMethod]
    public async Task Should_generate_feed_with_recent_and_reminisce_photos()
    {
        Mock<IPhotoRepository> photoRepository = new();
        AtomFeedGenerator generator = new(Mock.Of<ILogger<AtomFeedGenerator>>(), TimeProvider.System, photoRepository.Object);
        UserAccount user = new();
        UserFeedDetails userFeedDetails = new([1], [2, 3]);
        UserFeed userFeed = new() { UserAccount = user, UserFeedIdentifier = "test-id" };
        Photo photo1 = new() { Filename = "photo1.jpg", DateTaken = new(2026, 10, 1), Latitude = 1.1, Longitude = 1.2 };
        Photo photo2 = new() { Filename = "photo2.jpg", DateTaken = new(2026, 10, 2) };
        Photo photo3 = new()
        {
            Filename = "photo3.jpg",
            DateTaken = new(2026, 10, 3),
            Latitude = 3.1,
            Longitude = 3.2,
            GeoLocality = "local.3",
            GeoPrincipalSubdivision = "principal.3",
            GeoCity = "",
            GeoCountryName = "country.3"
        };
        photoRepository.Setup(x => x.GetAsync(user, 1)).ReturnsAsync(photo1);
        photoRepository.Setup(x => x.GetAsync(user, 2)).ReturnsAsync(photo2);
        photoRepository.Setup(x => x.GetAsync(user, 3)).ReturnsAsync(photo3);

        var (content, contentType, encoding) = await generator.GenerateFeedAsync("http://test.uri/base", user, userFeedDetails, userFeed);
        Assert.AreEqual("application/xml", contentType);
        Assert.AreEqual(Encoding.UTF8, encoding);
        Assert.IsNotNull(content);
        Assert.Contains("<link type=\"text/html\" href=\"http://test.uri/base\" rel=\"alternate\" />", content);
        Assert.Contains("<id>http://test.uri/base/api/feed/test-id</id>", content);
        Assert.Contains("<title>Small:Photos Recently added and Reminisce photos</title>", content);
        Assert.Contains("<a title=\"Taken: 01/10/2026 00:00:00\" href=\"http://test.uri/base/gallery/0/photo1.jpg\" target=\"_blank\"><img src=\"http://test.uri/base/photo/thumbnail/Large/0/photo1.jpg\" width=\"300\" height=\"300\" /></a>\n<div>01 Oct 2026 @ 00:00</div><div>Location: <a href=\"https://www.openstreetmap.org/?mlat=1.1&mlon=1.2&zoom=15\" target=\"_blank\">(1.1000, 1.2000)</a></div>", content);
        Assert.Contains("<a title=\"Taken: 02/10/2026 00:00:00\" href=\"http://test.uri/base/gallery/0/photo2.jpg\" target=\"_blank\"><img src=\"http://test.uri/base/photo/thumbnail/Large/0/photo2.jpg\" width=\"300\" height=\"300\" /></a>\n<div>02 Oct 2026 @ 00:00</div>", content);
        Assert.Contains("<a title=\"Taken: 03/10/2026 00:00:00\" href=\"http://test.uri/base/gallery/0/photo3.jpg\" target=\"_blank\"><img src=\"http://test.uri/base/photo/thumbnail/Large/0/photo3.jpg\" width=\"300\" height=\"300\" /></a>\n<div>03 Oct 2026 @ 00:00</div><div>Location: <a href=\"https://www.openstreetmap.org/?mlat=3.1&mlon=3.2&zoom=15\" target=\"_blank\">local.3</a></div>", content);
    }
}
