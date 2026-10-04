using System.ComponentModel.DataAnnotations;

namespace SmallPhotos.Model;

public class Photo
{
    public long PhotoId { get; set; }
    public long AlbumSourceId { get; set; }
    [Required]
    public AlbumSource? AlbumSource { get; set; }
    public string? Filename { get; set; }
    public string? RelativePath { get; set; }
    public DateTime FileCreationDateTime { get; set; }
    public DateTime FileModificationDateTime { get; set; }
    public DateTime? DateTaken { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdateDateTime { get; set; }
    public DateTime? DeletedDateTime { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool? IsGeoLookupCompleted { get; set; }
    public string? GeoLocality { get; set; }
    public string? GeoCity { get; set; }
    public string? GeoPrincipalSubdivision { get; set; }
    public string? GeoCountryName { get; set; }
}
