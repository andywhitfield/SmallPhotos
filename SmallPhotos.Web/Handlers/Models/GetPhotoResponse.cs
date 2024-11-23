using System;
using System.IO;

namespace SmallPhotos.Web.Handlers.Models;

public class GetPhotoResponse(Stream? imageStream, string? imageContentType, DateTime? imageLastModified, string? imageETag)
{
    public static readonly GetPhotoResponse Empty = new(null, null, null, null);

    public Stream? ImageStream { get; } = imageStream;
    public string? ImageContentType { get; } = imageContentType;
    public DateTime? ImageLastModified { get; } = imageLastModified;
    public string? ImageETag { get; } = imageETag;
}