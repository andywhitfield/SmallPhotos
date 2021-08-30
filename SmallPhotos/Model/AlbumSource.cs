using System;
using System.ComponentModel.DataAnnotations;

namespace SmallPhotos.Model
{
    public class AlbumSource
    {
        public int AlbumSourceId { get; set; }
        public int UserAccountId { get; set; }
        [Required]
        public UserAccount UserAccount { get; set; }
        public string Folder { get; set; }
        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdateDateTime { get; set; }
        public DateTime? DeletedDateTime { get; set; }
    }
}