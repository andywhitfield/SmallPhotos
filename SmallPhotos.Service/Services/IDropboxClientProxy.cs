using System.Threading.Tasks;
using Dropbox.Api.Files;
using Dropbox.Api.Stone;

namespace SmallPhotos.Service.Services;

public interface IDropboxClientProxy
{
    void Initialise(string? dropboxAccessToken, string? dropboxRefreshToken);
    Task<bool> RefreshAccessTokenAsync(string[] scopeList);
    Task<ListFolderResult> ListFolderAsync(string? path, bool recursive);
    Task<ListFolderResult> ListFolderContinueAsync(string cursor);
    Task<IDownloadResponse<FileMetadata>> DownloadAsync(string path);
}