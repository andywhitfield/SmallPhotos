using System.Collections.Generic;
using System.Threading.Tasks;
using SmallPhotos.Model;

namespace SmallPhotos.Data
{
    public interface IAlbumRepository
    {
        Task<List<AlbumSource>> GetAllAsync(UserAccount user);
        Task AddAsync(UserAccount userAccount, string folder);
        Task<AlbumSource?> GetAsync(UserAccount user, long albumSourceId);
        Task DeleteAlbumSourceAsync(AlbumSource albumSource);
    }
}