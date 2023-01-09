using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmallPhotos.Model;

namespace SmallPhotos.Data;

public class UserAccountRepository : IUserAccountRepository
{
    private readonly SqliteDataContext _context;

    public UserAccountRepository(SqliteDataContext context) => _context = context;

    public Task<UserAccount?> GetAsync(long userAccountId) =>
        _context.UserAccounts!.SingleOrDefaultAsync(a => a.UserAccountId == userAccountId && a.DeletedDateTime == null);

    public Task CreateNewUserAsync(ClaimsPrincipal user)
    {
        var authenticationUri = GetIdentifierFromPrincipal(user);
        _context.UserAccounts!.Add(new() { AuthenticationUri = authenticationUri });
        return _context.SaveChangesAsync();
    }

    private string? GetIdentifierFromPrincipal(ClaimsPrincipal user) => user?.FindFirstValue("sub");

    public async Task<UserAccount> GetUserAccountAsync(ClaimsPrincipal user) => (await GetUserAccountOrNullAsync(user)) ?? throw new ArgumentException($"No UserAccount for the user: {GetIdentifierFromPrincipal(user)}");

    public Task<UserAccount?> GetUserAccountOrNullAsync(ClaimsPrincipal user)
    {
        var authenticationUri = GetIdentifierFromPrincipal(user);
        if (string.IsNullOrWhiteSpace(authenticationUri))
            return Task.FromResult((UserAccount?)null);

        return _context.UserAccounts!.FirstOrDefaultAsync(ua => ua.AuthenticationUri == authenticationUri && ua.DeletedDateTime == null);
    }

    public Task<List<UserAccount>> GetAllAsync() => _context.UserAccounts!.Where(ua => ua.DeletedDateTime == null).ToListAsync();

    public Task UpdateAsync(UserAccount user)
    {
        user.LastUpdateDateTime = DateTime.UtcNow;
        return _context.SaveChangesAsync();
    }
}