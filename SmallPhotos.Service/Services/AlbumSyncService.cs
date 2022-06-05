using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Service.Models;

namespace SmallPhotos.Service.Services
{
    public class AlbumSyncService : IAlbumSyncService
    {
        private static readonly ISet<string> _supportedPhotoExtensions = new HashSet<string> { ".jpg", ".jpeg", ".gif", ".heic" };

        private readonly ILogger<AlbumSyncService> _logger;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IAlbumRepository _albumRepository;
        private readonly IPhotoRepository _photoRepository;
        private readonly IHttpClientFactory _httpClientFactory;

        public AlbumSyncService(
            ILogger<AlbumSyncService> logger,
            IUserAccountRepository userAccountRepository,
            IAlbumRepository albumRepository,
            IPhotoRepository photoRepository,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _userAccountRepository = userAccountRepository;
            _albumRepository = albumRepository;
            _photoRepository = photoRepository;
            _httpClientFactory = httpClientFactory;
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

                    _logger.LogDebug($"Checking albums changes for user [{user.UserAccountId}] / album [{albumSource.AlbumSourceId}:{albumSource.Folder}]");

                    var src = new DirectoryInfo(albumSource.Folder ?? "");
                    var filesInAlbum = (src.Exists ? src.EnumerateFiles() : Enumerable.Empty<FileInfo>())
                        .Where(f => _supportedPhotoExtensions.Contains(f.Extension.ToLowerInvariant()))
                        .ToList();

                    var photosInAlbum = await _photoRepository.GetAllAsync(albumSource);

                    _logger.LogDebug($"Files in folder: [{string.Join(',', filesInAlbum.Select(fi => fi.Name))}]");
                    _logger.LogDebug($"Photos in album: [{string.Join(',', photosInAlbum.Select(p => p.Filename))}]");

                    var newOrChangedPhotos = (
                        from f in filesInAlbum
                        join p in photosInAlbum on f.Name equals p.Filename into j
                        from m in j.DefaultIfEmpty()
                        where m == null || m.FileModificationDateTime < f.LastWriteTimeUtc
                        select f).ToList();

                    _logger.LogInformation($"New or changed photos in album: [{string.Join(',', newOrChangedPhotos.Select(fi => fi.Name))}]");

                    await Task.WhenAll(newOrChangedPhotos.Select(async newOrChanged =>
                    {
                        using var response = await httpClient.PostAsync("/api/photo", new StringContent(JsonSerializer.Serialize(
                            new CreateOrUpdatePhotoRequest { UserAccountId = user.UserAccountId, AlbumSourceId = albumSource.AlbumSourceId, Filename = newOrChanged.Name }),
                            Encoding.UTF8,
                            "application/json"));

                        var responseString = await response.Content.ReadAsStringAsync();
                        if (!response.IsSuccessStatusCode)
                            throw new InvalidOperationException($"Could not add/update photo [{newOrChanged.Name}] in album [{albumSource.AlbumSourceId}]: {responseString}");

                        _logger.LogInformation($"Successfully updated / added new photo: {responseString}");
                    }));

                    var deletedPhotos = (
                        from p in photosInAlbum
                        join f in filesInAlbum on p.Filename equals f.Name into j
                        from m in j.DefaultIfEmpty()
                        where m == null
                        select p
                    ).ToList();

                    _logger.LogInformation($"Deleted photos in album: [{string.Join(',', deletedPhotos.Select(p => p.Filename))}]");
                    foreach (var photo in deletedPhotos)
                        await _photoRepository.DeleteAsync(photo);
                }
            }

        }
    }
}