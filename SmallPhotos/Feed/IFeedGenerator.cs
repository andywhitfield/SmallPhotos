using System.Text;
using SmallPhotos.Model;

namespace SmallPhotos.Feed;

public interface IFeedGenerator
{
    Task<(string Content, string ContentType, Encoding Encoding)> GenerateFeedAsync(string baseUri, UserAccount user, UserFeedDetails userFeedDetails, UserFeed userFeed);
}
