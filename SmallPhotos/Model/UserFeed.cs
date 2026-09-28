using System.ComponentModel.DataAnnotations;

namespace SmallPhotos.Model;

public class UserFeed
{
    public long UserFeedId { get; set; }
    [Required]
    public required string UserFeedIdentifier { get; set; }
    public long UserAccountId { get; set; }
    [Required]
    public required UserAccount UserAccount { get; set; }
    public string? FeedDetails { get; set; }
    public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdateDateTime { get; set; }
    public DateTime? DeletedDateTime { get; set; }
}
