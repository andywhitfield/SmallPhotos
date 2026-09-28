using Microsoft.EntityFrameworkCore;
using SmallPhotos.Model;

namespace SmallPhotos.Data;

public class UserFeedRepository(SqliteDataContext context, TimeProvider timeProvider)
    : IUserFeedRepository
{
    public Task<UserFeed?> FindByIdentifierAsync(string feedIdentifier) =>
        context.UserFeeds.SingleOrDefaultAsync(f => f.UserFeedIdentifier == feedIdentifier && f.DeletedDateTime == null);

    public Task SaveAsync(UserFeed userFeed)
    {
        userFeed.LastUpdateDateTime = timeProvider.GetUtcNow().DateTime;
        return context.SaveChangesAsync();
    }
}
