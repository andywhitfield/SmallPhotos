using System;
using System.IO;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Stone;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace SmallPhotos.Dropbox;

public class DropboxClientProxy : IDropboxClientProxy, IDisposable
{
    private readonly ILogger<DropboxClientProxy> _logger;
    private DropboxClient? _dropboxClient;
    private readonly IOptionsSnapshot<DropboxOptions> _dropboxOptions;
    private readonly Lazy<DirectoryInfo> _dropboxTempDir;

    public DropboxClientProxy(ILogger<DropboxClientProxy> logger, IOptionsSnapshot<DropboxOptions> dropboxOptions)
    {
        _logger = logger;
        _dropboxOptions = dropboxOptions;
        _dropboxTempDir = new(CreateTemporaryDirectory);
    }

    private DropboxClient Client => _dropboxClient ?? throw new InvalidOperationException("DropboxClient not initialised");

    public void Initialise(string? dropboxAccessToken, string? dropboxRefreshToken)
    {
        DisposeDropboxClient();
        if (_dropboxTempDir.IsValueCreated && !_dropboxTempDir.Value.Exists)
        {
            _logger.LogTrace($"Re-initialised, re-creating dropbox temp dir: {_dropboxTempDir.Value.FullName}");
            _dropboxTempDir.Value.Create();
        }

        _dropboxClient = new(dropboxAccessToken, dropboxRefreshToken, _dropboxOptions.Value.SmallPhotosAppKey, _dropboxOptions.Value.SmallPhotosAppSecret, new());
    }

    public Task<bool> RefreshAccessTokenAsync(string[] scopeList) =>
        Client.RefreshAccessToken(scopeList);

    public Task<ListFolderResult> ListFolderAsync(string? path, bool recursive) =>
        Client.Files.ListFolderAsync(path, recursive: recursive);

    public Task<ListFolderResult> ListFolderContinueAsync(string cursor) =>
        Client.Files.ListFolderContinueAsync(cursor);

    public Task<IDownloadResponse<FileMetadata>> DownloadAsync(string path) =>
        Client.Files.DownloadAsync(path);

    public DirectoryInfo TemporaryDownloadDirectory => _dropboxTempDir.Value;

    public void Dispose() => DisposeDropboxClient();

    private DirectoryInfo CreateTemporaryDirectory()
    {
        DirectoryInfo tempDir = new(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        _logger.LogTrace($"Creating dropbox temp dir: {tempDir.FullName}");
        tempDir.Create();
        return tempDir;
    }

    private void DisposeDropboxClient()
    {
        _dropboxClient?.Dispose();
        if (_dropboxTempDir.IsValueCreated && _dropboxTempDir.Value.Exists)
        {
            _logger.LogTrace($"Deleting temporary Dropbox download directory: {_dropboxTempDir.Value.FullName}");
            try
            {
                Policy
                    .Handle<Exception>()
                    .WaitAndRetry(3, retry => TimeSpan.FromSeconds(retry), (ex, ts) => _logger.LogWarning(ex, $"Error deleting temporary download directory [{_dropboxTempDir.Value.FullName}], trying again in {ts}"))
                    .Execute(() => _dropboxTempDir.Value.Delete(true));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not delete the temporary folder used for Dropbox downloads, is there a file left open somewhere? This folder will not be automatically cleaned-up. Path: {_dropboxTempDir.Value.FullName}");
            }
        }
    }
}