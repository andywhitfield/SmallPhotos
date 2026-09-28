using SmallPhotos.Model;

namespace SmallPhotos.Data;

public interface IUserFeedRepository
{
    Task<UserFeed?> FindByIdentifierAsync(string feedIdentifier);
    Task<UserFeed?> GetAsync(UserAccount user);
    Task CreateAsync(UserAccount user, string uniqueFeedIdentifier);
    Task SaveAsync(UserFeed userFeed, DateTime? lastUpdateDateTime);
}
