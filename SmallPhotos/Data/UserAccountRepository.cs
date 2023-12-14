using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallPhotos.Model;

namespace SmallPhotos.Data;

public class UserAccountRepository(ILogger<UserAccountRepository> logger, SqliteDataContext context) : IUserAccountRepository
{
    public Task<UserAccount?> GetAsync(long userAccountId) =>
        context.UserAccounts!.SingleOrDefaultAsync(a => a.UserAccountId == userAccountId && a.DeletedDateTime == null);

    public Task CreateNewUserAsync(ClaimsPrincipal user)
    {
        var authenticationUri = GetIdentifierFromPrincipal(user) ?? throw new InvalidOperationException($"Could not get auth id from user: [{string.Join(',', user.Claims.Select(c => $"{c.Type}={c.Value}"))}]");
        logger.LogInformation($"Creating new user with uri [{authenticationUri}]");
        context.UserAccounts!.Add(new() { AuthenticationUri = authenticationUri });
        return context.SaveChangesAsync();
    }

    private string? GetIdentifierFromPrincipal(ClaimsPrincipal user) => user?.FindFirstValue("name");

    public async Task<UserAccount> GetUserAccountAsync(ClaimsPrincipal user) => (await GetUserAccountOrNullAsync(user)) ?? throw new ArgumentException($"No UserAccount for the user: {GetIdentifierFromPrincipal(user)}");

    public Task<UserAccount?> GetUserAccountOrNullAsync(ClaimsPrincipal user)
    {
        var authenticationUri = GetIdentifierFromPrincipal(user);
        if (string.IsNullOrWhiteSpace(authenticationUri))
            return Task.FromResult((UserAccount?)null);

        return context.UserAccounts!.FirstOrDefaultAsync(ua => ua.AuthenticationUri == authenticationUri && ua.DeletedDateTime == null);
    }

    public Task<List<UserAccount>> GetAllAsync() => context.UserAccounts!.Where(ua => ua.DeletedDateTime == null).ToListAsync();

    public Task UpdateAsync(UserAccount user)
    {
        user.LastUpdateDateTime = DateTime.UtcNow;
        return context.SaveChangesAsync();
    }
}