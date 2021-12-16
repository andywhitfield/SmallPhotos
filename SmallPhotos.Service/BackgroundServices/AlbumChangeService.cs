using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmallPhotos.Service.BackgroundServices
{
    public class AlbumChangeService : BackgroundService
    {
        private readonly ILogger<AlbumChangeService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public AlbumChangeService(ILogger<AlbumChangeService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Running album change background service");
            try
            {
                await Task.Delay(1000, stoppingToken);
                do
                {
                    TimeSpan pollPeriod;
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<AlbumChangeServiceOptions>>();
                        pollPeriod = options.Value.PollPeriod;

                        // get all sources
                        // get all photos in sources & work out what is new / deleted / updated
                        // delete any
                        // for update / new, call an endpoint in the service to generate the thumbnail & save
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
                _logger.LogError(ex, "An error occurred running the album change background service");
            }
        }
    }
}