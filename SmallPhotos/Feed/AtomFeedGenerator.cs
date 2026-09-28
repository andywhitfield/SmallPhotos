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

            if (userFeedDetails.RecentlyAddedPhotoIds.Length > 0)
            {
                itemDescription.Append("<h2>Recently added photos</h2><p>");
                foreach (var recentlyAddedPhotoId in userFeedDetails.RecentlyAddedPhotoIds)
                {
                    var recentlyAddedPhoto = await photoRepository.GetAsync(user, recentlyAddedPhotoId);
                    if (recentlyAddedPhoto == null)
                    {
                        logger.LogWarning("Could not find photo with id {PhotoId} for user {UserId}", recentlyAddedPhotoId, user.UserAccountId);
                        continue;
                    }

                    itemDescription.Append($"""
<div><a title="Taken: {recentlyAddedPhoto.DateTaken}" href="{baseUri}/gallery/${recentlyAddedPhoto.PhotoId}/${recentlyAddedPhoto.Filename}"><img src="{baseUri}/photo/thumbnail/${ThumbnailSize.Large}/${recentlyAddedPhoto.PhotoId}/${recentlyAddedPhoto.Filename}" width="${recentlyAddedPhoto.Width}" height="${recentlyAddedPhoto.Height}" /></a>
    <div>${(recentlyAddedPhoto.DateTaken ?? recentlyAddedPhoto.CreatedDateTime).ToString("dd MMM yyyy @ HH:mm")}</div>
</div>
""");
                }
                itemDescription.Append("</p>");
            }

            // TODO: dupe from above
            if (userFeedDetails.ReminiscePhotoIds.Length > 0)
            {
                itemDescription.Append("<h2>Reminisce photos</h2><p>");
                foreach (var reminiscePhotoId in userFeedDetails.ReminiscePhotoIds)
                {
                    var reminiscePhoto = await photoRepository.GetAsync(user, reminiscePhotoId);
                    if (reminiscePhoto == null)
                    {
                        logger.LogWarning("Could not find photo with id {PhotoId} for user {UserId}", reminiscePhotoId, user.UserAccountId);
                        continue;
                    }

                    itemDescription.Append($"""
<div><a title="Taken: {reminiscePhoto.DateTaken}" href="{baseUri}/gallery/${reminiscePhoto.PhotoId}/${reminiscePhoto.Filename}"><img src="{baseUri}/photo/thumbnail/${ThumbnailSize.Large}/${reminiscePhoto.PhotoId}/${reminiscePhoto.Filename}" width="${reminiscePhoto.Width}" height="${reminiscePhoto.Height}" /></a>
    <div>${(reminiscePhoto.DateTaken ?? reminiscePhoto.CreatedDateTime).ToString("dd MMM yyyy @ HH:mm")}</div>
</div>
""");
                }
                itemDescription.Append("</p>");
            }

            content.Add(new XCData($"{itemDescription}<p><a href=\"{baseUri}\">Open Small:Photos</a></p>"));
            entry.Add(content);

            docRoot.Add(entry);
        }

        atom.Add(docRoot);
        return (ToXmlString(atom), "application/xml", Encoding.UTF8);
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
