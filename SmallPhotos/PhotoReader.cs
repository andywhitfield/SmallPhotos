using System;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.AspNetCore.StaticFiles;

namespace SmallPhotos;

public class PhotoReader(IContentTypeProvider contentTypeProvider)
    : IPhotoReader
{
    public async Task<(string? ContentType, Stream? ContentStream)> GetPhotoStreamForWebAsync(FileInfo file)
    {
        if (!contentTypeProvider.TryGetContentType(file.Name, out var contentType) &&
            (contentType = IsHeic(file) ? "image/jpeg" : null) == null)
        {
            return (null, null);
        }

        return (contentType, IsHeic(file) ? (await ConvertToJpegAsync(file)) : file.OpenRead());
    }

    private bool IsHeic(FileInfo file) => string.Equals(file.Extension, ".heic", StringComparison.OrdinalIgnoreCase);

    private async Task<Stream> ConvertToJpegAsync(FileInfo file)
    {
        MemoryStream jpegStream = new();
        using (MagickImage image = new(file))
            await image.WriteAsync(jpegStream, MagickFormat.Jpeg);
            
        jpegStream.Position = 0;
        return jpegStream;
    }
}