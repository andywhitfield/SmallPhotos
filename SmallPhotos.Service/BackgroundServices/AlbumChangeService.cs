using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmallPhotos.Service.Services;

namespace SmallPhotos.Service.BackgroundServices;

public class AlbumChangeService : BackgroundService
{
    private readonly ILogger<AlbumChangeService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public AlbumChangeService(ILogger<AlbumChangeService> logger, IServiceScopeFactory serviceScopeFactory, IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _applicationLifetime = applicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Running album change background service");
        try
        {
            TaskCompletionSource<object> waitForStart = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _applicationLifetime.ApplicationStarted.Register(obj =>
            {
                var tcs = obj as TaskCompletionSource<object>;
                tcs?.TrySetResult(0);
            }, waitForStart);

            _logger.LogDebug("Waiting for application start");
            await waitForStart.Task;

            _logger.LogDebug("Application started, waiting 1s before inital sync");
            await Task.Delay(1000, stoppingToken);

            do
            {
                TimeSpan pollPeriod;
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    pollPeriod = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<AlbumChangeServiceOptions>>().Value.PollPeriod;
                    await scope.ServiceProvider.GetRequiredService<IAlbumSyncService>().SyncAllAsync(stoppingToken);
                }

                _logger.LogInformation($"Album change service - waiting [{pollPeriod}] before running again");
                await Task.Delay(pollPeriod, stoppingToken);
            } while (!stoppingToken.IsCancellationRequested);
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("Album change background service cancellation token cancelled - service stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred running the album change background service - stopping background service!");
        }
    }
}