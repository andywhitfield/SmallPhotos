using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers;

public class TaggedPhotoHomeRequestHandler(IUserAccountRepository userAccountRepository, IPhotoRepository photoRepository)
    : IRequestHandler<TaggedPhotoHomeRequest, IEnumerable<(string Tag, int PhotoCount)>>
{
    public async Task<IEnumerable<(string Tag, int PhotoCount)>> Handle(TaggedPhotoHomeRequest request, CancellationToken cancellationToken)
    {
        var user = await userAccountRepository.GetUserAccountAsync(request.User);
        return await photoRepository.GetTagsAndCountAsync(user);
    }
}