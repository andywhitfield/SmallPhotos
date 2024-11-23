using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers;

public class UpdateUserThumbnailSizeRequestHandler(IUserAccountRepository userAccountRepository)
: IRequestHandler<UpdateUserThumbnailSizeRequest, bool>
{
    public async Task<bool> Handle(UpdateUserThumbnailSizeRequest request, CancellationToken cancellationToken)
    {
        var user = await userAccountRepository.GetUserAccountAsync(request.User);
        if (user.ThumbnailSize != request.NewThumbnailSize)
        {
            user.ThumbnailSize = request.NewThumbnailSize;
            await userAccountRepository.UpdateAsync(user);
        }

        return true;            
    }
}