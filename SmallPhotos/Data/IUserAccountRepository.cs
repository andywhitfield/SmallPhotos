using System.Security.Claims;
using System.Threading.Tasks;
using SmallPhotos.Model;

namespace SmallPhotos.Data
{
    public interface IUserAccountRepository
    {
        Task CreateNewUserAsync(ClaimsPrincipal user);
        Task<UserAccount> GetAsync(long userAccountId);
        Task<UserAccount> GetUserAccountAsync(ClaimsPrincipal user);
        Task<UserAccount> GetUserAccountOrNullAsync(ClaimsPrincipal user);
    }
}