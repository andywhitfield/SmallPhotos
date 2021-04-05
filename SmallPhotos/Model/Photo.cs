using System;
using System.ComponentModel.DataAnnotations;

namespace SmallPhotos.Model
{
    public class Photo
    {
        public int PhotoId { get; set; }
        public int AlbumSourceId { get; set; }
        [Required]
        public string AlbumSource { get; set; }
        public string Filename { get; set; }
        public DateTime FileCreationDateTime { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdateDateTime { get; set; }
        public DateTime? DeletedDateTime { get; set; }
    }
}