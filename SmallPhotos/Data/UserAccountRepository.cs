using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallPhotos.Model;

namespace SmallPhotos.Data
{
    public class UserAccountRepository : IUserAccountRepository
    {
        private readonly ILogger<UserAccountRepository> _logger;
        private readonly SqliteDataContext _context;

        public UserAccountRepository(ILogger<UserAccountRepository> logger, SqliteDataContext context)
        {
            _logger = logger;
            _context = context;
        }

        public Task<UserAccount> GetAsync(long userAccountId) =>
            _context.UserAccounts.SingleOrDefaultAsync(a => a.UserAccountId == userAccountId && a.DeletedDateTime == null);

        public Task CreateNewUserAsync(ClaimsPrincipal user)
        {
            var authenticationUri = GetIdentifierFromPrincipal(user);
            var newUser = new UserAccount { AuthenticationUri = authenticationUri };

            _context.UserAccounts.Add(newUser);
            return _context.SaveChangesAsync();
        }

        private string GetIdentifierFromPrincipal(ClaimsPrincipal user) => user?.FindFirstValue("sub");

        public Task<UserAccount> GetUserAccountAsync(ClaimsPrincipal user) => GetUserAccountOrNullAsync(user) ?? throw new ArgumentException($"No UserAccount for the user: {GetIdentifierFromPrincipal(user)}");

        public Task<UserAccount> GetUserAccountOrNullAsync(ClaimsPrincipal user)
        {
            var authenticationUri = GetIdentifierFromPrincipal(user);
            if (string.IsNullOrWhiteSpace(authenticationUri))
                return null;

            return _context.UserAccounts.FirstOrDefaultAsync(ua => ua.AuthenticationUri == authenticationUri && ua.DeletedDateTime == null);
        }
    }
}