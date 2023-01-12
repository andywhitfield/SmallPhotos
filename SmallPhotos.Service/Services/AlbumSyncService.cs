using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;

namespace SmallPhotos.Service.Services;

public class AlbumSyncService : IAlbumSyncService
{
    private readonly ILogger<AlbumSyncService> _logger;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAlbumRepository _albumRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFilesystemSync _filesystemSync;
    private readonly IDropboxSync _dropboxSync;

    public AlbumSyncService(
        ILogger<AlbumSyncService> logger,
        IUserAccountRepository userAccountRepository,
        IAlbumRepository albumRepository,
        IHttpClientFactory httpClientFactory,
        IFilesystemSync filesystemSync,
        IDropboxSync dropboxSync)
    {
        _logger = logger;
        _userAccountRepository = userAccountRepository;
        _albumRepository = albumRepository;
        _httpClientFactory = httpClientFactory;
        _filesystemSync = filesystemSync;
        _dropboxSync = dropboxSync;
    }

    public async Task SyncAllAsync(CancellationToken stoppingToken)
    {
        using var httpClient = _httpClientFactory.CreateClient(Startup.BackgroundServiceHttpClient);
        if (httpClient.BaseAddress == null)
        {
            _logger.LogError("Background http client not configured - cannot sync!");
            return;
        }

        foreach (var user in await _userAccountRepository.GetAllAsync())
        {
            stoppingToken.ThrowIfCancellationRequested();

            _logger.LogDebug($"Checking albums changes for user [{user.UserAccountId}]");

            foreach (var albumSource in await _albumRepository.GetAllAsync(user))
            {
                stoppingToken.ThrowIfCancellationRequested();

                if (albumSource.IsDropboxSource)
                    await _dropboxSync.SyncAsync(albumSource, user, httpClient);
                else
                    await _filesystemSync.SyncAsync(albumSource, user, httpClient);

                _logger.LogInformation($"Completed album sync [{albumSource.AlbumSourceId}|{albumSource.Folder}] for user [{user.UserAccountId}]");
            }
        }
    }
}