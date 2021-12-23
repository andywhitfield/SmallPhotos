using System;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.AspNetCore.StaticFiles;

namespace SmallPhotos
{
    public class PhotoReader : IPhotoReader
    {
        private readonly IContentTypeProvider _contentTypeProvider;
        public PhotoReader(IContentTypeProvider contentTypeProvider) => _contentTypeProvider = contentTypeProvider;

        public async Task<(string ContentType, Stream ContentStream)> GetPhotoStreamForWebAsync(FileInfo file)
        {
            if (!_contentTypeProvider.TryGetContentType(file.Name, out var contentType) &&
                (contentType = IsHeic(file) ? "image/jpeg" : null) == null)
            {
                return (null, null);
            }

            return (contentType, IsHeic(file) ? (await ConvertToJpegAsync(file)) : file.OpenRead());
        }

        private bool IsHeic(FileInfo file) => string.Equals(file.Extension, ".heic", StringComparison.OrdinalIgnoreCase);

        private async Task<Stream> ConvertToJpegAsync(FileInfo file)
        {
            var jpegStream = new MemoryStream();
            using (var image = new MagickImage(file))
                await image.WriteAsync(jpegStream, MagickFormat.Jpeg);
                
            jpegStream.Position = 0;
            return jpegStream;
        }
    }
}