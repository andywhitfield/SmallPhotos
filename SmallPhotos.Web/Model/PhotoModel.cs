using System.Drawing;

namespace SmallPhotos.Web.Model;

public class PhotoModel(long photoId, string source, bool isDropboxSource, string filename,
    string filepath, Size size, DateTime dateTaken, DateTime fileCreationDate,
    bool isStarred, IEnumerable<string> tags, double? latitude, double? longitude,
    string? geoLocality, string? geoCity, string? geoPrincipal, string? geoCountry)
{
    private const string _dateFormat = "HH:mm' on 'dd MMMM yyyy";
    private const string _dateFormatShort = "dd MMM yyyy @ HH:mm";

    public long PhotoId { get; } = photoId;
    public string Source { get; } = source;
    public bool IsDropboxSource { get; } = isDropboxSource;
    public string Filename { get; } = filename;
    public string Filepath { get; } = filepath;
    public Size Size { get; } = size;
    public string SizeInfo { get; } = $"{size.Width}w x {size.Height}h";
    public DateTime DateTimeTaken { get; } = dateTaken;
    public string DateTaken { get; } = dateTaken.ToString(_dateFormat);
    public string DateTakenShort { get; } = dateTaken.ToString(_dateFormatShort);
    public string FileCreationDate { get; } = fileCreationDate.ToString(_dateFormat);
    public bool IsStarred { get; } = isStarred;
    public IEnumerable<string> Tags { get; } = tags;
    public bool HasGeoCoordinates { get; } = latitude != null && longitude != null;
    public double? GeoLatitude { get; } = latitude;
    public double? GeoLongitude { get; } = longitude;
    public string GeoLocation { get; } = GetGeoLocation(latitude, longitude, geoLocality, geoCity, geoPrincipal, geoCountry);
    public string GeoLocationFull { get; } = GetGeoLocationFull(latitude, longitude, geoLocality, geoCity, geoPrincipal, geoCountry);

    private static string GetGeoLocation(double? latitude, double? longitude, string? geoLocality, string? geoCity,
        string? geoPrincipal, string? geoCountry)
    {
        if (latitude == null || longitude == null)
            return "";

        if (string.IsNullOrEmpty(geoLocality) && string.IsNullOrEmpty(geoCity) &&
            string.IsNullOrEmpty(geoPrincipal) && string.IsNullOrEmpty(geoCountry))
        {
            return $"({latitude.Value:N4}, {longitude.Value:N4})";
        }

        if (!string.IsNullOrEmpty(geoLocality))
            return geoLocality;
        if (!string.IsNullOrEmpty(geoCity))
            return geoCity;
        if (!string.IsNullOrEmpty(geoPrincipal))
            return geoPrincipal;
        return geoCountry ?? "";
    }

    private static string GetGeoLocationFull(double? latitude, double? longitude, string? geoLocality, string? geoCity,
        string? geoPrincipal, string? geoCountry)
    {
        if (latitude == null || longitude == null)
            return "";

        if (string.IsNullOrEmpty(geoLocality) && string.IsNullOrEmpty(geoCity) &&
            string.IsNullOrEmpty(geoPrincipal) && string.IsNullOrEmpty(geoCountry))
        {
            return $"Coordinates ({latitude.Value:N4}, {longitude.Value:N4})";
        }

        return string.Join(", ", new[] { geoLocality, geoCity, geoPrincipal, geoCountry }.Where(s => !string.IsNullOrEmpty(s)));
    }
}
