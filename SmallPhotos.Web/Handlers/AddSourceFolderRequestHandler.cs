using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SmallPhotos.Data;
using SmallPhotos.Web.Handlers.Models;

namespace SmallPhotos.Web.Handlers;

public class AddSourceFolderRequestHandler(
    ILogger<AddSourceFolderRequestHandler> logger,
    IUserAccountRepository userAccountRepository,
    IAlbumRepository albumRepository)
    : IRequestHandler<AddSourceFolderRequest, bool>
{
    public async Task<bool> Handle(AddSourceFolderRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Folder))
            return false;

        if (!string.IsNullOrEmpty(request.DropboxAccessToken) && !string.IsNullOrEmpty(request.DropboxRefreshToken))
            return await HandleDropboxFolder(request);

        if (!Path.IsPathRooted(request.Folder))
        {
            logger.LogWarning("Cannot add a folder which isn't a fully-qualified path [{RequestFolder}]", request.Folder);
            return false;
        }

        var user = await userAccountRepository.GetUserAccountAsync(request.User);
        var currentSources = await albumRepository.GetAllAsync(user);

        var directoryToAdd = Path.GetFullPath(request.Folder);
        if (currentSources.Where(s => !s.IsDropboxSource).Any(s => string.Equals(Path.GetFullPath(s.Folder ?? ""), directoryToAdd, StringComparison.OrdinalIgnoreCase)))
        {
            // TODO: this just ends up returning a 400 to the client - should actually show a reason on the UI
            logger.LogWarning("The requested folder to add [{RequestFolder}] already exists, can't add a duplicate", request.Folder);
            return false;
        }

        await albumRepository.AddAsync(user, request.Folder, request.Recursive);
        return true;
    }

    private async Task<bool> HandleDropboxFolder(AddSourceFolderRequest request)
    {
        var user = await userAccountRepository.GetUserAccountAsync(request.User);
        var currentSources = await albumRepository.GetAllAsync(user);

        if (currentSources.Where(s => s.IsDropboxSource).Any(s => string.Equals(s.Folder ?? "", request.Folder, StringComparison.OrdinalIgnoreCase)))
        {
            // TODO: this just ends up returning a 400 to the client - should actually show a reason on the UI
            logger.LogWarning("The requested folder to add [{RequestFolder}] already exists, can't add a duplicate", request.Folder);
            return false;
        }

        await albumRepository.AddAsync(user, request.Folder, request.Recursive, request.DropboxAccessToken, request.DropboxRefreshToken);
        return true;
    }
}