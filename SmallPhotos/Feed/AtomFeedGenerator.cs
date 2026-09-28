using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Model;

namespace SmallPhotos.Feed;

public class AtomFeedGenerator(
    ILogger<AtomFeedGenerator> logger,
    TimeProvider timeProvider,
    IPhotoRepository photoRepository
)
    : IFeedGenerator
{
    private readonly XNamespace ns = "http://www.w3.org/2005/Atom";

    public async Task<(string Content, string ContentType, Encoding Encoding)> GenerateFeedAsync(string baseUri, UserAccount user, UserFeedDetails userFeedDetails, UserFeed userFeed)
    {
        var atom = new XDocument(new XDeclaration("1.0", "utf-8", null));
        var docRoot = new XElement(ns + "feed");
        var todayDt = timeProvider.GetUtcNow().Date;
        var today = DateOnly.FromDateTime(todayDt);

        docRoot.Add(new XElement(ns + "title", "Small:Photos"));
        docRoot.Add(new XElement(ns + "link", new XAttribute("type", "text/html"), new XAttribute("href", baseUri), new XAttribute("rel", "alternate")));
        docRoot.Add(new XElement(ns + "updated", (userFeed.LastUpdateDateTime ?? userFeed.CreatedDateTime).ToString("O")));
        docRoot.Add(new XElement(ns + "id", $"{baseUri}/api/feed/{userFeed.UserFeedIdentifier}"));

        if (userFeedDetails.RecentlyAddedPhotoIds.Length > 0 || userFeedDetails.ReminiscePhotoIds.Length > 0)
        {
            var entry = new XElement(ns + "entry");
            var itemUri = $"{baseUri}/api/feed/{userFeed.UserFeedIdentifier}/{today:yyyyMMdd}";
            entry.Add(new XElement(ns + "title", $"Small:Photos {Title(userFeedDetails)}"));
            entry.Add(new XElement(ns + "link", new XAttribute("href", itemUri)));
            entry.Add(new XElement(ns + "updated", (userFeed.LastUpdateDateTime ?? userFeed.CreatedDateTime).ToString("O")));
            entry.Add(new XElement(ns + "id", itemUri));

            var content = new XElement(ns + "content", new XAttribute("type", "html"));
            StringBuilder itemDescription = new();

            await AppendPhotosAsync(itemDescription, user, baseUri, userFeedDetails.RecentlyAddedPhotoIds, "Recently added photos");
            await AppendPhotosAsync(itemDescription, user, baseUri, userFeedDetails.ReminiscePhotoIds, "Reminisce photos");

            content.Add(new XCData($"{itemDescription}<p><a href=\"{baseUri}\" target=\"_blank\">Open Small:Photos</a></p>"));
            entry.Add(content);

            docRoot.Add(entry);
        }

        atom.Add(docRoot);
        return (ToXmlString(atom), "application/xml", Encoding.UTF8);
    }

    private async Task AppendPhotosAsync(StringBuilder itemDescription, UserAccount user, string baseUri, long[] photoIds, string title)
    {
        if (photoIds.Length > 0)
        {
            itemDescription.Append("<h2>").Append(title).Append("</h2><p>");

            foreach (var photoId in photoIds)
            {
                var photo = await photoRepository.GetAsync(user, photoId);
                if (photo == null)
                {
                    logger.LogWarning("Could not find photo with id {PhotoId} for user {UserId}", photoId, user.UserAccountId);
                    continue;
                }

                itemDescription.Append($"""
<div><a title="Taken: {photo.DateTaken}" href="{baseUri}/gallery/{photo.PhotoId}/{photo.Filename}" target="_blank"><img src="{baseUri}/photo/thumbnail/{ThumbnailSize.Large}/{photo.PhotoId}/{photo.Filename}" width="{photo.Width}" height="{photo.Height}" /></a>
<div>{(photo.DateTaken ?? photo.CreatedDateTime).ToString("dd MMM yyyy @ HH:mm")}</div>
</div>
""");
            }
            itemDescription.Append("</p>");
        }
    }

    private static string Title(UserFeedDetails userFeedDetails)
    {
        if (userFeedDetails.RecentlyAddedPhotoIds.Length > 0 && userFeedDetails.ReminiscePhotoIds.Length > 0)
            return "Recently added and Reminisce photos";
        if (userFeedDetails.RecentlyAddedPhotoIds.Length > 0)
            return "Recently added photos";
        if (userFeedDetails.ReminiscePhotoIds.Length > 0)
            return "Reminisce photos";
        return "";
    }

    private static string ToXmlString(XDocument xml)
    {
        using var writer = new Utf8StringWriter();
        xml.Save(writer);
        return writer.ToString();
    }

    private class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
