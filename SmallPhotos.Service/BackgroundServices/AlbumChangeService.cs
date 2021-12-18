using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmallPhotos.Data;

namespace SmallPhotos.Service.BackgroundServices
{
    public class AlbumChangeService : BackgroundService
    {
        private static readonly ISet<string> _supportedPhotoExtensions = new HashSet<string> { ".jpg", ".jpeg", ".gif", ".heic" };
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
                        var userRepository = scope.ServiceProvider.GetRequiredService<IUserAccountRepository>();
                        var albumRepository = scope.ServiceProvider.GetRequiredService<IAlbumRepository>();
                        var photoRepository = scope.ServiceProvider.GetRequiredService<IPhotoRepository>();

                        pollPeriod = options.Value.PollPeriod;

                        // get all sources
                        // get all photos in sources & work out what is new / deleted / updated
                        // delete any
                        // for update / new, call an endpoint in the service to generate the thumbnail & save

                        foreach (var user in await userRepository.GetAllAsync())
                        {
                            _logger.LogDebug($"Checking albums changes for user [{user.UserAccountId}]");

                            foreach (var albumSource in await albumRepository.GetAllAsync(user))
                            {
                                _logger.LogDebug($"Checking albums changes for user [{user.UserAccountId}] / album [{albumSource.AlbumSourceId}:{albumSource.Folder}]");

                                var src = new DirectoryInfo(albumSource.Folder);
                                var filesInAlbum = (src.Exists ? src.EnumerateFiles() : Enumerable.Empty<FileInfo>())
                                    .Where(f => _supportedPhotoExtensions.Contains(f.Extension.ToLowerInvariant()))
                                    .ToList();

                                var photosInAlbum = await photoRepository.GetAllAsync(albumSource);

                                _logger.LogDebug($"Files in folder: [{string.Join(',', filesInAlbum.Select(fi => fi.Name))}]");
                                _logger.LogDebug($"Photos in album: [{string.Join(',', photosInAlbum.Select(p => p.Filename))}]");

                                var newOrChangedPhotos = (
                                    from f in filesInAlbum
                                    join p in photosInAlbum on f.Name equals p.Filename into j
                                    from m in j.DefaultIfEmpty()
                                    where m == null || m.FileModificationDateTime < f.LastWriteTimeUtc
                                    select new { File = f, Photo = m }).ToList();
                                
                                _logger.LogInformation($"New or changed photos in album: [{string.Join(',', newOrChangedPhotos.Select(fi => fi.File.Name))}]");

                                // TODO - temp, just add an entry for now...need to call an endpoint which
                                //        does the thumbnail, and image parsing
                                foreach (var newOrChanged in newOrChangedPhotos)
                                {
                                    if (newOrChanged.Photo == null)
                                        await photoRepository.AddAsync(albumSource, newOrChanged.File);
                                    else
                                        await photoRepository.UpdateAsync(newOrChanged.Photo, newOrChanged.File);
                                }

                                var deletedPhotos = (
                                    from p in photosInAlbum
                                    join f in filesInAlbum on p.Filename equals f.Name into j
                                    from m in j.DefaultIfEmpty()
                                    where m == null
                                    select p
                                ).ToList();
                                
                                _logger.LogInformation($"Deleted photos in album: [{string.Join(',', deletedPhotos.Select(p => p.Filename))}]");
                                foreach (var photo in deletedPhotos)
                                    await photoRepository.DeleteAsync(photo);
                            }
                        }
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