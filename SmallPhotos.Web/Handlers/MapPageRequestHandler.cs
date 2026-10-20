using MediatR;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;
using SmallPhotos.Web.Model;

namespace SmallPhotos.Web.Handlers;

public class MapPageRequestHandler(
    ILogger<MapPageRequestHandler> logger,
    IUserAccountRepository userAccountRepository,
    IPhotoRepository photoRepository)
    : IRequestHandler<MapPageRequest, MapPageResponse>
{
    public async Task<MapPageResponse> Handle(MapPageRequest request, CancellationToken cancellationToken)
    {
        var user = await userAccountRepository.GetUserAccountOrNullAsync(request.User);
        if (user == null)
        {
            logger.LogInformation("No active user account, user [{RequestUserIdentityName}] is not valid", request.User.Identity?.Name);
            return new(false, []);
        }

        return new(true, [default(PhotoModel)]);
    }
}