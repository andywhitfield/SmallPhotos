using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers;

public class SaveViewOptionsRequestHandler(IUserAccountRepository userAccountRepository)
    : IRequestHandler<SaveViewOptionsRequest, bool>
{
    public async Task<bool> Handle(SaveViewOptionsRequest request, CancellationToken cancellationToken)
    {
        var anyChanges = false;
        var user = await userAccountRepository.GetUserAccountAsync(request.User);
        if (user.ThumbnailSize != request.NewThumbnailSize)
        {
            user.ThumbnailSize = request.NewThumbnailSize;
            anyChanges = true;
        }

        if (user.GalleryImagePageSize != request.NewPageSize)
        {
            user.GalleryImagePageSize = request.NewPageSize;
            anyChanges = true;
        }
        
        if (anyChanges)
            await userAccountRepository.UpdateAsync(user);
            
        return true;    
    }
}