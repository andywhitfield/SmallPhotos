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

public class AddSourceFolderRequestHandler : IRequestHandler<AddSourceFolderRequest, bool>
{
    private readonly ILogger<AddSourceFolderRequestHandler> _logger;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAlbumRepository _albumRepository;

    public AddSourceFolderRequestHandler(
        ILogger<AddSourceFolderRequestHandler> logger,
        IUserAccountRepository userAccountRepository,
        IAlbumRepository albumRepository)
    {
        _logger = logger;
        _userAccountRepository = userAccountRepository;
        _albumRepository = albumRepository;
    }

    public async Task<bool> Handle(AddSourceFolderRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Folder))
            return false;

        if (!string.IsNullOrEmpty(request.DropboxAccessToken) && !string.IsNullOrEmpty(request.DropboxRefreshToken))
            return await HandleDropboxFolder(request);

        if (!Path.IsPathRooted(request.Folder))
        {
            _logger.LogWarning($"Cannot add a folder which isn't a fully-qualified path [{request.Folder}]");
            return false;
        }

        var user = await _userAccountRepository.GetUserAccountAsync(request.User);
        var currentSources = await _albumRepository.GetAllAsync(user);

        var directoryToAdd = Path.GetFullPath(request.Folder);
        if (currentSources.Where(s => !s.IsDropboxSource).Any(s => string.Equals(Path.GetFullPath(s.Folder ?? ""), directoryToAdd, StringComparison.OrdinalIgnoreCase)))
        {
            // TODO: this just ends up returning a 400 to the client - should actually show a reason on the UI
            _logger.LogWarning($"The requested folder to add [{request.Folder}] already exists, can't add a duplicate");
            return false;
        }

        await _albumRepository.AddAsync(user, request.Folder, request.Recursive);
        return true;
    }

    private async Task<bool> HandleDropboxFolder(AddSourceFolderRequest request)
    {
        var user = await _userAccountRepository.GetUserAccountAsync(request.User);
        var currentSources = await _albumRepository.GetAllAsync(user);

        if (currentSources.Where(s => s.IsDropboxSource).Any(s => string.Equals(s.Folder ?? "", request.Folder, StringComparison.OrdinalIgnoreCase)))
        {
            // TODO: this just ends up returning a 400 to the client - should actually show a reason on the UI
            _logger.LogWarning($"The requested folder to add [{request.Folder}] already exists, can't add a duplicate");
            return false;
        }

        await _albumRepository.AddAsync(user, request.Folder, request.Recursive, request.DropboxAccessToken, request.DropboxRefreshToken);
        return true;
    }
}