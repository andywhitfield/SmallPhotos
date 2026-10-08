using System.Threading.RateLimiting;
using BigDataCloud;
using Microsoft.Extensions.Options;
using SmallPhotos.Data;
using SmallPhotos.Service.BackgroundServices;

namespace SmallPhotos.Service.Services;

public class GeoLocationUpdateService(
    ILogger<GeoLocationUpdateService> logger,
    IOptionsSnapshot<GeoLocationServiceOptions> options,
    IPhotoRepository photoRepository,
    RateLimiter rateLimiter,
    BigDataCloudClient bigDataCloudClient)
    : IGeoLocationUpdateService
{
    public async Task UpdateAsync(CancellationToken stoppingToken)
    {
        logger.LogDebug("Getting photos needing geo location updates...");
        var updateCount = 0;
        await foreach (var photo in photoRepository.GetWithGeoLookupRequiredAsync(options.Value.PhotoUpdateLimitPerRun))
        {
            stoppingToken.ThrowIfCancellationRequested();
            using var lease = await rateLimiter.AcquireAsync(cancellationToken: stoppingToken);
            if (!lease.IsAcquired)
                throw new InvalidOperationException("Error handling throttling");

            stoppingToken.ThrowIfCancellationRequested();

            try
            {
                logger.LogInformation("Photo {PhotoId} needs geo location info, calling BigDataCloud api", photo.PhotoId);
                var geocodeResponse = await bigDataCloudClient.ReverseGeocoding.ReverseGeocodeAsync(photo.Latitude!.Value, photo.Longitude!.Value, "en", stoppingToken);
                if (geocodeResponse == null)
                {
                    logger.LogWarning("No location found for photo [{PhotoId}] ({Latitude}, {Longitude})", photo.PhotoId, photo.Latitude, photo.Longitude);
                }
                else
                {
                    logger.LogInformation("Got geo location for photo [{PhotoId}] ({Latitude}, {Longitude}): locality=[{Locality}], city=[{City}], principal=[{Principal}], country=[{Country}]",
                        photo.PhotoId, photo.Latitude, photo.Longitude, geocodeResponse.Locality, geocodeResponse.City, geocodeResponse.PrincipalSubdivision, geocodeResponse.CountryName);
                    photo.GeoLocality = geocodeResponse.Locality;
                    photo.GeoCity = geocodeResponse.City;
                    photo.GeoPrincipalSubdivision = geocodeResponse.PrincipalSubdivision;
                    photo.GeoCountryName = geocodeResponse.CountryName;
                }
            }
            finally
            {
                photo.IsGeoLookupCompleted = true;
                await photoRepository.SaveAsync(photo);
                updateCount++;
            }
        }
        logger.LogDebug("Updated geo location on {Count} photos", updateCount);
    }
}
