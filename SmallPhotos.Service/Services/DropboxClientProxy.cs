using System;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Stone;
using Microsoft.Extensions.Options;

namespace SmallPhotos.Service.Services;

public class DropboxClientProxy : IDropboxClientProxy, IDisposable
{
    private DropboxClient? _dropboxClient;
    private readonly IOptionsSnapshot<DropboxOptions> _dropboxOptions;

    public DropboxClientProxy(IOptionsSnapshot<DropboxOptions> dropboxOptions)
    {
        _dropboxOptions = dropboxOptions;
    }

    private DropboxClient Client => _dropboxClient ?? throw new InvalidOperationException("DropboxClient not initialised");

    public void Initialise(string? dropboxAccessToken, string? dropboxRefreshToken)
    {
        _dropboxClient?.Dispose();
        _dropboxClient = new(dropboxAccessToken, dropboxRefreshToken, _dropboxOptions.Value.SmallPhotosAppKey, _dropboxOptions.Value.SmallPhotosAppSecret, new DropboxClientConfig());
    }

    public Task<bool> RefreshAccessTokenAsync(string[] scopeList) =>
        Client.RefreshAccessToken(scopeList);
    
    public Task<ListFolderResult> ListFolderAsync(string? path, bool recursive) =>
        Client.Files.ListFolderAsync(path, recursive: recursive);
    
    public Task<ListFolderResult> ListFolderContinueAsync(string cursor) =>
        Client.Files.ListFolderContinueAsync(cursor);
    
    public Task<IDownloadResponse<FileMetadata>> DownloadAsync(string path) =>
        Client.Files.DownloadAsync(path);

    public void Dispose() => _dropboxClient?.Dispose();
}