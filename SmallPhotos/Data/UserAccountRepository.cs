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

    public async Task<UserAccount> CreateNewUserAsync(string email, byte[] credentialId, byte[] publicKey, byte[] userHandle)
    {
        logger.LogInformation($"Creating new user with email [{email}]");
        if (await context.UserAccounts!.AnyAsync(ua => ua.Email == email))
            throw new InvalidOperationException($"UserAccount already exists with email [{email}]");

        var newUserAccount = context.UserAccounts!.Add(new() { Email = email });
        context.UserAccountCredentials!.Add(new() { UserAccount = newUserAccount.Entity,
            CredentialId = credentialId, PublicKey = publicKey, UserHandle = userHandle });
        await context.SaveChangesAsync();

        return newUserAccount.Entity;
    }

    private string? GetEmailFromPrincipal(ClaimsPrincipal user)
    {
        logger.LogTrace($"Getting email from user: {user?.Identity?.Name}: [{string.Join(',', user?.Claims.Select(c => $"{c.Type}={c.Value}") ?? Enumerable.Empty<string>())}]");
        return user?.FindFirstValue(ClaimTypes.Name);
    }

    public async Task<UserAccount> GetUserAccountAsync(ClaimsPrincipal user) => (await GetUserAccountOrNullAsync(user)) ?? throw new ArgumentException($"No UserAccount for the user: {GetEmailFromPrincipal(user)}");

    public Task<UserAccount?> GetUserAccountOrNullAsync(ClaimsPrincipal user)
    {
        var email = GetEmailFromPrincipal(user);
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogInformation($"No user account found with email [{email}]");
            return Task.FromResult((UserAccount?)null);
        }

        logger.LogTrace($"Looking up user account with email [{email}]");
        return context.UserAccounts!.FirstOrDefaultAsync(ua => ua.Email == email && ua.DeletedDateTime == null);
    }

    public Task<UserAccount?> GetUserAccountByEmailAsync(string email) => context.UserAccounts!.FirstOrDefaultAsync(a => a.Email == email);

    public Task<List<UserAccount>> GetAllAsync() => context.UserAccounts!.Where(ua => ua.DeletedDateTime == null).ToListAsync();

    public Task UpdateAsync(UserAccount user)
    {
        user.LastUpdateDateTime = DateTime.UtcNow;
        return context.SaveChangesAsync();
    }

    public Task UpdateAsync(UserAccountCredential userAccountCredential)
    {
        userAccountCredential.LastUpdateDateTime = DateTime.UtcNow;
        return context.SaveChangesAsync();
    }

    public IAsyncEnumerable<UserAccountCredential> GetUserAccountCredentialsAsync(UserAccount user)
        => context.UserAccountCredentials!.Where(uac => uac.UserAccountId == user.UserAccountId).AsAsyncEnumerable();
    
    public Task<UserAccountCredential?> GetUserAccountCredentialsByUserHandleAsync(byte[] userHandle)
        => context.UserAccountCredentials!.FirstOrDefaultAsync(uac => uac.UserHandle.SequenceEqual(userHandle));
}