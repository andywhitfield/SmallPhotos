using SmallPhotos.Model;

namespace SmallPhotos.Data;

public interface IUserFeedRepository
{
    Task<UserFeed?> FindByIdentifierAsync(string feedIdentifier);
    Task SaveAsync(UserFeed userFeed);
}
