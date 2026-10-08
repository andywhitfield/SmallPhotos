using Microsoft.Extensions.Options;
using SmallPhotos.Service.Services;

namespace SmallPhotos.Service.BackgroundServices;

public class GeoLocationService(
    ILogger<GeoLocationService> logger,
    IServiceScopeFactory serviceScopeFactory,
    IHostApplicationLifetime applicationLifetime)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Running geo location background service");
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

            TimeSpan GetInitialDelayConfigValue()
            {
                using var scope = serviceScopeFactory.CreateScope();
                return scope.ServiceProvider.GetRequiredService<IOptions<GeoLocationServiceOptions>>().Value.InitialDelay;
            }
            var initialDelay = GetInitialDelayConfigValue();

            logger.LogDebug("Application started, waiting {InitialDelay} before running geo location service", initialDelay);
            await Task.Delay(initialDelay, stoppingToken);

            var consecutiveFailures = 0;
            do
            {
                logger.LogInformation("Geo location service starting");

                TimeSpan pollPeriod;
                using (var scope = serviceScopeFactory.CreateScope())
                {
                    var geoLocationServiceOptions = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<GeoLocationServiceOptions>>().Value;
                    pollPeriod = geoLocationServiceOptions.PollPeriod;
                    if (geoLocationServiceOptions.Enabled)
                    {
                        try
                        {
                            await scope.ServiceProvider.GetRequiredService<IGeoLocationUpdateService>().UpdateAsync(stoppingToken);
                            consecutiveFailures = 0;
                        }
                        catch (Exception ex)
                        {
                            consecutiveFailures++;
                            if (consecutiveFailures > 4)
                            {
                                logger.LogError(ex, "An error occurred updating geo location. There have been {ConsecutiveFailures} consecutive failures - giving up!", consecutiveFailures);
                                throw;
                            }
                            else
                            {
                                logger.LogError(ex, "An error occurred updateing geo location");
                            }
                        }
                    }
                    else
                    {
                        logger.LogInformation("Geo location service disabled, not running.");
                    }
                }

                logger.LogInformation("Geo location service complete - waiting [{PollPeriod}] before running again", pollPeriod);
                await Task.Delay(pollPeriod, stoppingToken);
            } while (!stoppingToken.IsCancellationRequested);
        }
        catch (TaskCanceledException)
        {
            logger.LogDebug("Geo location background service cancellation token cancelled - service stopping");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred running the geo location background service - stopping background service!");
        }
    }
}
