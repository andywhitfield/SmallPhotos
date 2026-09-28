using System.Text.Json;

namespace SmallPhotos.Feed;

public record UserFeedDetails(int[] RecentlyAddedPhotoIds, int[] ReminiscePhotoIds);

public static class UserFeedDetailsExtensions
{
    private static readonly UserFeedDetails Empty = new([], []);

    public static UserFeedDetails ToUserFeedDetails(this string? userFeedDetails)
    {
        if (string.IsNullOrEmpty(userFeedDetails))
            return Empty;

        return JsonSerializer.Deserialize<UserFeedDetails>(userFeedDetails, JsonSerializerOptions.Default) ?? Empty;
    }

    public static string ToJson(this UserFeedDetails? userFeedDetails)
        => JsonSerializer.Serialize(userFeedDetails ?? Empty, JsonSerializerOptions.Default);
}
