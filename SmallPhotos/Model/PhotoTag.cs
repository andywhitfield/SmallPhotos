using System;
using System.ComponentModel.DataAnnotations;

namespace SmallPhotos.Model;

public class PhotoTag
{
    public long PhotoTagId { get; set; }
    public long UserAccountId { get; set; }
    [Required]
    public UserAccount? UserAccount { get; set; }
    public long PhotoId { get; set; }
    [Required]
    public Photo? Photo { get; set; }
    [Required]
    public string Tag { get; set; } = "";
    public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
}