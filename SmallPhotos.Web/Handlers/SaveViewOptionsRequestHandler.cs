using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers;

public class SaveViewOptionsRequestHandler : IRequestHandler<SaveViewOptionsRequest, bool>
{
    private readonly IUserAccountRepository _userAccountRepository;

    public SaveViewOptionsRequestHandler(IUserAccountRepository userAccountRepository) => _userAccountRepository = userAccountRepository;

    public async Task<bool> Handle(SaveViewOptionsRequest request, CancellationToken cancellationToken)
    {
        var anyChanges = false;
        var user = await _userAccountRepository.GetUserAccountAsync(request.User);
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
            await _userAccountRepository.UpdateAsync(user);
            
        return true;    
    }
}