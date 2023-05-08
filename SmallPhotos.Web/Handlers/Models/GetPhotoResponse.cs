using System;
using System.IO;

namespace SmallPhotos.Web.Handlers.Models;

public class GetPhotoResponse
{
    public static readonly GetPhotoResponse Empty = new(null, null, null, null);
    
    public GetPhotoResponse(Stream? imageStream, string? imageContentType, DateTime? imageLastModified, string? imageETag)
    {
        ImageStream = imageStream;
        ImageContentType = imageContentType;
        ImageLastModified = imageLastModified;
        ImageETag = imageETag;
    }

    public Stream? ImageStream { get; }
    public string? ImageContentType { get; }
    public DateTime? ImageLastModified { get; }
    public string? ImageETag { get; }
}