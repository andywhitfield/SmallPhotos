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
using Microsoft.Extensions.Options;
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Service.BackgroundServices;
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
        private readonly IOptionsSnapshot<AlbumChangeServiceOptions> _options;

        public AlbumSyncService(
            ILogger<AlbumSyncService> logger,
            IUserAccountRepository userAccountRepository,
            IAlbumRepository albumRepository,
            IPhotoRepository photoRepository,
            IHttpClientFactory httpClientFactory,
            IOptionsSnapshot<AlbumChangeServiceOptions> options)
        {
            _logger = logger;
            _userAccountRepository = userAccountRepository;
            _albumRepository = albumRepository;
            _photoRepository = photoRepository;
            _httpClientFactory = httpClientFactory;
            _options = options;
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

                    var filesInAlbum = GetFilesForAlbumSource(albumSource);

                    var photosInAlbum = await _photoRepository.GetAllAsync(albumSource);

                    _logger.LogDebug($"Files in folder: [{string.Join(',', filesInAlbum.Select(fi => fi.Name))}]");
                    _logger.LogDebug($"Photos in album: [{string.Join(',', photosInAlbum.Select(p => p.Filename))}]");

                    var newOrChangedPhotos = (
                        from f in filesInAlbum
                        join p in photosInAlbum on new { Filename = f.Name, RelativePath = albumSource.Folder.GetRelativePath(f) } equals new { p.Filename, RelativePath = string.IsNullOrEmpty(p.RelativePath) ? "" : p.RelativePath } into j
                        from m in j.DefaultIfEmpty()
                        where m == null || m.FileModificationDateTime < f.LastWriteTimeUtc
                        select f).ToList();

                    _logger.LogInformation($"New or changed photos in album: [{string.Join(',', newOrChangedPhotos.Select(fi => fi.Name))}]");

                    foreach (var requestBatch in newOrChangedPhotos.Chunk(_options.Value.SyncPhotoBatchSize))
                    {
                        await Task.WhenAll(requestBatch.Select(async newOrChanged =>
                        {
                            using var response = await httpClient.PostAsync("/api/photo", new StringContent(JsonSerializer.Serialize(
                                new CreateOrUpdatePhotoRequest { UserAccountId = user.UserAccountId, AlbumSourceId = albumSource.AlbumSourceId, Filename = newOrChanged.Name, FilePath = albumSource.Folder.GetRelativePath(newOrChanged) }),
                                Encoding.UTF8,
                                "application/json"));

                            var responseString = await response.Content.ReadAsStringAsync();
                            if (!response.IsSuccessStatusCode)
                                throw new InvalidOperationException($"Could not add/update photo [{newOrChanged.Name}] in album [{albumSource.AlbumSourceId}]: {responseString}");

                            _logger.LogInformation($"Successfully updated / added new photo: {responseString}");
                        }));
                    }

                    var deletedPhotos = (
                        from p in photosInAlbum
                        join f in filesInAlbum on p.Filename equals f.Name into j
                        from m in j.DefaultIfEmpty()
                        where m == null
                        select p
                    ).ToList();

                    _logger.LogInformation($"Deleting photos in album: [{string.Join(',', deletedPhotos.Select(p => p.Filename))}]");
                    foreach (var photo in deletedPhotos)
                        await _photoRepository.DeleteAsync(photo);
                }
            }
        }

        private IEnumerable<FileInfo> GetFilesForAlbumSource(AlbumSource albumSource) =>
            GetPhotoFilesInDirectory(new DirectoryInfo(albumSource.Folder ?? ""), albumSource.RecurseSubFolders ?? false);

        private IEnumerable<FileInfo> GetPhotoFilesInDirectory(DirectoryInfo dir, bool recurse)
        {
            if (!dir.Exists)
                yield break;

            foreach (var file in dir.EnumerateFiles().Where(f => _supportedPhotoExtensions.Contains(f.Extension.ToLowerInvariant())))
                yield return file;

            if (recurse)
            {
                foreach (var subDir in dir.EnumerateDirectories())
                    foreach (var file in GetPhotoFilesInDirectory(subDir, recurse))
                        yield return file;
            }
        }
    }
}