using System.Collections.Generic;
using System.Threading.Tasks;
using SmallPhotos.Model;

namespace SmallPhotos.Data
{
    public interface IAlbumRepository
    {
        Task<List<AlbumSource>> GetAllSourcesAsync(UserAccount user);
        Task AddAlbumSourceAsync(UserAccount userAccount, string folder);
        Task<AlbumSource> GetAlbumSourceAsync(UserAccount user, int albumSourceId);
        Task DeleteAlbumSourceAsync(AlbumSource albumSource);
    }
}