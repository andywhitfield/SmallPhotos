using System.Threading;
using System.Threading.Tasks;

namespace SmallPhotos.Service.Services;

public interface IAlbumSyncService
{
    Task SyncAllAsync(CancellationToken stoppingToken);
}