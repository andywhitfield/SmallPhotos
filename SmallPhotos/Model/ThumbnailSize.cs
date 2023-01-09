using System;
using System.Drawing;

namespace SmallPhotos.Model;

public enum ThumbnailSize
{
    Small = 0,
    Medium = 1,
    Large = 2
}

public static class ThumbnailSizeExtension
{
    public static Size ToSize(this ThumbnailSize thumbnailSize) =>
        thumbnailSize switch
        {
            ThumbnailSize.Small => new(100, 100),
            ThumbnailSize.Medium => new(200, 200),
            ThumbnailSize.Large => new(300, 300),
            _ => throw new ArgumentOutOfRangeException(nameof(thumbnailSize))
        };
}