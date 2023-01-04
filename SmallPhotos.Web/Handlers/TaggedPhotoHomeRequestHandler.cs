using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers;

public class TaggedPhotoHomeRequestHandler : IRequestHandler<TaggedPhotoHomeRequest, IEnumerable<(string Tag, int PhotoCount)>>
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IPhotoRepository _photoRepository;

    public TaggedPhotoHomeRequestHandler(IUserAccountRepository userAccountRepository, IPhotoRepository photoRepository)
    {
        _userAccountRepository = userAccountRepository;
        _photoRepository = photoRepository;
    }

    public async Task<IEnumerable<(string Tag, int PhotoCount)>> Handle(TaggedPhotoHomeRequest request, CancellationToken cancellationToken)
    {
        var user = await _userAccountRepository.GetUserAccountAsync(request.User);
        return await _photoRepository.GetTagsAndCountAsync(user);
    }
}