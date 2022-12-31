using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers;

public class UpdateUserThumbnailSizeRequestHandler : IRequestHandler<UpdateUserThumbnailSizeRequest, bool>
{
    private readonly IUserAccountRepository _userAccountRepository;

    public UpdateUserThumbnailSizeRequestHandler(IUserAccountRepository userAccountRepository) => _userAccountRepository = userAccountRepository;

    public async Task<bool> Handle(UpdateUserThumbnailSizeRequest request, CancellationToken cancellationToken)
    {
        var user = await _userAccountRepository.GetUserAccountAsync(request.User);
        if (user.ThumbnailSize != request.NewThumbnailSize)
        {
            user.ThumbnailSize = request.NewThumbnailSize;
            await _userAccountRepository.UpdateAsync(user);
        }

        return true;            
    }
}