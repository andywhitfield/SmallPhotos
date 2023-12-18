using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SmallPhotos.Model;

[Index(nameof(Email), IsUnique = true)]
public class UserAccount
{
    public long UserAccountId { get; set; }
    [Required]
    public string? Email { get; set; }
    public ThumbnailSize ThumbnailSize { get; set; }
    public int? GalleryImagePageSize { get; set; }
    public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdateDateTime { get; set; }
    public DateTime? DeletedDateTime { get; set; }
}