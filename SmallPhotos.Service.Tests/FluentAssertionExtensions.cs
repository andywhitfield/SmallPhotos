using System.Drawing;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Collections;
using ImageMagick;

namespace SmallPhotos.Service.Tests;

public static class FluentAssertionExtensions
{
    public static AndConstraint<GenericCollectionAssertions<byte>> BeOfSize(this GenericCollectionAssertions<byte> imageBytes, Size expectedImageSize)
    {
        using MagickImage image = new(imageBytes.Subject.ToArray());
        image.Width.Should().Be((uint)expectedImageSize.Width);
        image.Height.Should().Be((uint)expectedImageSize.Height);
        return new(imageBytes);
    }
}