namespace SmallPhotos.Service.Services;

public interface IGeoLocationUpdateService
{
    Task UpdateAsync(CancellationToken stoppingToken);
}
