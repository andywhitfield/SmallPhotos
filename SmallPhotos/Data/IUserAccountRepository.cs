using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using SmallPhotos.Model;

namespace SmallPhotos.Data;

public interface IUserAccountRepository
{
    Task<UserAccount> CreateNewUserAsync(string email, byte[] credentialId, byte[] publicKey, byte[] userHandle);
    Task<UserAccount?> GetAsync(long userAccountId);
    Task<UserAccount?> GetUserAccountByEmailAsync(string email);
    Task<UserAccount> GetUserAccountAsync(ClaimsPrincipal user);
    Task<UserAccount?> GetUserAccountOrNullAsync(ClaimsPrincipal user);
    Task<List<UserAccount>> GetAllAsync();
    Task UpdateAsync(UserAccount user);
    Task UpdateAsync(UserAccountCredential userAccountCredential);
    IAsyncEnumerable<UserAccountCredential> GetUserAccountCredentialsAsync(UserAccount user);
    Task<UserAccountCredential?> GetUserAccountCredentialsByUserHandleAsync(byte[] userHandle);
}