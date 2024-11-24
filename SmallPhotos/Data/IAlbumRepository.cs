using System.Collections.Generic;
using System.Threading.Tasks;
using SmallPhotos.Model;

namespace SmallPhotos.Data;

public interface IAlbumRepository
{
    Task<List<AlbumSource>> GetAllAsync(UserAccount user);
    Task AddAsync(UserAccount userAccount, string folder, bool recursive, string? dropboxAccessToken = null, string? dropboxRefreshToken = null);
    Task<AlbumSource?> GetAsync(UserAccount user, long albumSourceId);
    Task UpdateAsync(AlbumSource albumSource);
    Task DeleteAlbumSourceAsync(AlbumSource albumSource);
}