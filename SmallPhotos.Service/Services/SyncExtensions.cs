using System.Collections.Generic;

namespace SmallPhotos.Service.Services;

public static class SyncExtensions
{
    public static ISet<string> SupportedPhotoExtensions = new HashSet<string> { ".jpg", ".jpeg", ".gif", ".heic" };
}