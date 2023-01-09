using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SmallPhotos.Data;
using SmallPhotos.Model;
using SmallPhotos.Service.Models;

namespace SmallPhotos.Service.Services;

public static class SyncExtensions
{
    public static ISet<string> SupportedPhotoExtensions = new HashSet<string> { ".jpg", ".jpeg", ".gif", ".heic" };

    public static async Task<string> PostCreateOrUpdatePhotoAsync(this HttpClient httpClient, UserAccount user, AlbumSource albumSource, string? filename, string? relativeFolder)
    {
        using var response = await httpClient.PostAsync("/api/photo", new StringContent(JsonSerializer.Serialize(
            new CreateOrUpdatePhotoRequest { UserAccountId = user.UserAccountId, AlbumSourceId = albumSource.AlbumSourceId, Filename = filename, FilePath = relativeFolder }),
            Encoding.UTF8,
            "application/json"));

        var responseString = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Could not add/update photo [{filename}] in album [{albumSource.AlbumSourceId}]: {responseString}");
        
        return responseString;
    }

    public static async Task DeletePhotosAsync(this ICollection<Photo> photos, IPhotoRepository photoRepository)
    {
        foreach (var photo in photos)
            await photoRepository.DeleteAsync(photo);
    }
}