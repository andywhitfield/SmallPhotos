using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers
{
    public class GetProfileRequestHandler : IRequestHandler<GetProfileRequest, GetProfileResponse>
    {
        private readonly ILogger<GetProfileRequestHandler> _logger;

        public GetProfileRequestHandler(ILogger<GetProfileRequestHandler> logger) => _logger = logger;

        public Task<GetProfileResponse> Handle(GetProfileRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new GetProfileResponse());
        }
    }
}