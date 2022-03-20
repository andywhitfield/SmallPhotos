using System.Drawing;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Collections;
using ImageMagick;

namespace SmallPhotos.Service.Tests
{
    public static class FluentAssertionExtensions
    {
        public static AndConstraint<GenericCollectionAssertions<byte>> BeOfSize(this GenericCollectionAssertions<byte> imageBytes, Size expectedImageSize)
        {
            using var image = new MagickImage(imageBytes.Subject.ToArray());
            image.Width.Should().Be(expectedImageSize.Width);
            image.Height.Should().Be(expectedImageSize.Height);
            return new AndConstraint<GenericCollectionAssertions<byte>>(imageBytes);
        }
    }
}