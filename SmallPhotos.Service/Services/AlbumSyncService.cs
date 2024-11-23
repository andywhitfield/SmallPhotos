using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;

namespace SmallPhotos.Service.Services;

public class AlbumSyncService(
    ILogger<AlbumSyncService> logger,
    IUserAccountRepository userAccountRepository,
    IAlbumRepository albumRepository,
    IHttpClientFactory httpClientFactory,
    IFilesystemSync filesystemSync,
    IDropboxSync dropboxSync)
    : IAlbumSyncService
{
    public async Task SyncAllAsync(CancellationToken stoppingToken)
    {
        using var httpClient = httpClientFactory.CreateClient(Startup.BackgroundServiceHttpClient);
        if (httpClient.BaseAddress == null)
        {
            logger.LogError("Background http client not configured - cannot sync!");
            return;
        }

        foreach (var user in await userAccountRepository.GetAllAsync())
        {
            stoppingToken.ThrowIfCancellationRequested();

            logger.LogDebug("Checking albums changes for user [{UserAccountId}]", user.UserAccountId);

            foreach (var albumSource in await albumRepository.GetAllAsync(user))
            {
                stoppingToken.ThrowIfCancellationRequested();

                if (albumSource.IsDropboxSource)
                    await dropboxSync.SyncAsync(albumSource, user, httpClient);
                else
                    await filesystemSync.SyncAsync(albumSource, user, httpClient);

                logger.LogInformation("Completed album sync [{AlbumSourceId}|{albumSourceFolder}] for user [{UserAccountId}]",
                    albumSource.AlbumSourceId, albumSource.Folder, user.UserAccountId);
            }
        }
    }
}