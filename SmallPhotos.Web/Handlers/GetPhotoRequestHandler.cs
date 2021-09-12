using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using MediatR;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers
{
    public class GetPhotoRequestHandler : IRequestHandler<GetPhotoRequest, GetPhotoResponse>
    {
        private readonly ILogger<GetPhotoRequestHandler> _logger;
        private readonly IUserAccountRepository _userAccountRepository;

        public GetPhotoRequestHandler(ILogger<GetPhotoRequestHandler> logger, IUserAccountRepository userAccountRepository)
        {
            _logger = logger;
            _userAccountRepository = userAccountRepository;
        }

        public async Task<GetPhotoResponse> Handle(GetPhotoRequest request, CancellationToken cancellationToken)
        {
            var user = await _userAccountRepository.GetUserAccountAsync(request.User);
            // TODO: load photo by id, validate user & name

            var file = "/Users/andywhitfield/Dropbox/Camera uploads/2021-09-12 10.22.31.heic";
            var jpegStream = new MemoryStream();
            using (var image = new MagickImage(file, MagickFormat.Heic))
                await image.WriteAsync(jpegStream, MagickFormat.Jpeg);

            jpegStream.Position = 0;
            return new GetPhotoResponse(jpegStream);
        }
    }
}