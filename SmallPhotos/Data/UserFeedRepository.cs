using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallPhotos.Model;

namespace SmallPhotos.Data;

public class UserFeedRepository(ILogger<UserFeedRepository> logger, SqliteDataContext context, TimeProvider timeProvider)
    : IUserFeedRepository
{
    public Task<UserFeed?> FindByIdentifierAsync(string feedIdentifier)
        => context.UserFeeds.SingleOrDefaultAsync(f => f.UserFeedIdentifier == feedIdentifier && f.DeletedDateTime == null);

    public Task<UserFeed?> GetAsync(UserAccount user)
        => context.UserFeeds.SingleOrDefaultAsync(f => f.UserAccountId == user.UserAccountId && f.DeletedDateTime == null);

    public Task CreateAsync(UserAccount user, string uniqueFeedIdentifier)
    {
        logger.LogInformation("Creating new feed for user {userAccountId}, {uniqueFeedIdentifier}", user.UserAccountId, uniqueFeedIdentifier);

        context.UserFeeds.Add(new()
        {
            UserAccount = user,
            UserFeedIdentifier = uniqueFeedIdentifier
        });

        return context.SaveChangesAsync();
    }

    public Task SaveAsync(UserFeed userFeed, DateTime? lastUpdateDateTime)
    {
        userFeed.LastUpdateDateTime = lastUpdateDateTime ?? timeProvider.GetUtcNow().DateTime;
        return context.SaveChangesAsync();
    }
}
