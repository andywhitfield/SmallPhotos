using MediatR;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers;

public class UpdateUserThumbnailDetailsRequestHandler(IUserAccountRepository userAccountRepository)
: IRequestHandler<UpdateUserThumbnailDetailsRequest, bool>
{
    public async Task<bool> Handle(UpdateUserThumbnailDetailsRequest request, CancellationToken cancellationToken)
    {
        var user = await userAccountRepository.GetUserAccountAsync(request.User);
        if (user.GalleryShowDetails != request.ShowThumbnailDetails)
        {
            user.GalleryShowDetails = request.ShowThumbnailDetails;
            await userAccountRepository.UpdateAsync(user);
        }

        return true;            
    }
}