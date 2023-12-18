using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using SmallPhotos.Model;

namespace SmallPhotos.Data;

public interface IUserAccountRepository
{
    Task CreateNewUserAsync(string email, byte[] credentialId, byte[] publicKey, byte[] userHandle);
    Task<UserAccount?> GetAsync(long userAccountId);
    Task<UserAccount?> GetUserAccountByCredentialIdAsync(byte[] credentialId);
    Task<UserAccount> GetUserAccountAsync(ClaimsPrincipal user);
    Task<UserAccount?> GetUserAccountOrNullAsync(ClaimsPrincipal user);
    Task<List<UserAccount>> GetAllAsync();
    Task UpdateAsync(UserAccount user);
}