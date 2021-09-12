using System.Threading;
using System.Threading.Tasks;
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

            return new GetPhotoResponse(new System.IO.FileInfo("/Users/andywhitfield/Dropbox/Camera uploads/2020-08-04 12.12.40.jpg"));
        }
    }
}