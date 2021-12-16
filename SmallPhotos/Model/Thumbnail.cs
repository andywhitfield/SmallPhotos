using System;
using System.ComponentModel.DataAnnotations;

namespace SmallPhotos.Model
{
    public class Thumbnail
    {
        public long ThumbnailId { get; set; }
        public long PhotoId { get; set; }
        [Required]
        public Photo Photo { get; set; }
        public byte[] ThumbnailImage { get; set; }
        public ThumbnailSize ThumbnailSize { get; set; }
        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdateDateTime { get; set; }
    }
}