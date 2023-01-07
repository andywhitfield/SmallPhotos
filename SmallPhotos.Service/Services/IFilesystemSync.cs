using System.Net.Http;
using System.Threading.Tasks;
using SmallPhotos.Model;

namespace SmallPhotos.Service.Services;

public interface IFilesystemSync
{
    Task SyncAsync(AlbumSource albumSource, UserAccount user, HttpClient httpClient);
}