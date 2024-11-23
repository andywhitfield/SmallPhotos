using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmallPhotos.Service.Services;

namespace SmallPhotos.Service.BackgroundServices;

public class AlbumChangeService(ILogger<AlbumChangeService> logger, IServiceScopeFactory serviceScopeFactory,
    IHostApplicationLifetime applicationLifetime)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Running album change background service");
        try
        {
            TaskCompletionSource<object> waitForStart = new(TaskCreationOptions.RunContinuationsAsynchronously);
            applicationLifetime.ApplicationStarted.Register(obj =>
            {
                var tcs = obj as TaskCompletionSource<object>;
                tcs?.TrySetResult(0);
            }, waitForStart);

            logger.LogDebug("Waiting for application start");
            await waitForStart.Task;

            logger.LogDebug("Application started, waiting 1s before inital sync");
            await Task.Delay(1000, stoppingToken);

            var consecutiveFailures = 0;
            do
            {
                logger.LogInformation("Album change service sync starting");

                TimeSpan pollPeriod;
                using (var scope = serviceScopeFactory.CreateScope())
                {
                    pollPeriod = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<AlbumChangeServiceOptions>>().Value.PollPeriod;

                    try
                    {
                        await scope.ServiceProvider.GetRequiredService<IAlbumSyncService>().SyncAllAsync(stoppingToken);
                        consecutiveFailures = 0;
                    }
                    catch (Exception ex)
                    {
                        consecutiveFailures++;
                        if (consecutiveFailures > 4)
                        {
                            logger.LogError(ex, "An error occurred syncing photos. There have been {ConsecutiveFailures} consecutive failures - giving up!", consecutiveFailures);
                            throw;
                        }
                        else
                        {
                            logger.LogError(ex, "An error occurred syncing photos");
                        }
                    }
                }

                logger.LogInformation("Album change service sync complete - waiting [{PollPeriod}] before running again", pollPeriod);
                await Task.Delay(pollPeriod, stoppingToken);
            } while (!stoppingToken.IsCancellationRequested);
        }
        catch (TaskCanceledException)
        {
            logger.LogDebug("Album change background service cancellation token cancelled - service stopping");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred running the album change background service - stopping background service!");
        }
    }
}