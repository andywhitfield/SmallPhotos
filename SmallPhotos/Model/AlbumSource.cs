using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmallPhotos.Model;

public class AlbumSource
{
    public long AlbumSourceId { get; set; }
    public long UserAccountId { get; set; }
    [Required]
    public UserAccount? UserAccount { get; set; }
    public string? Folder { get; set; }
    public bool? RecurseSubFolders { get; set; }
    public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdateDateTime { get; set; }
    public DateTime? DeletedDateTime { get; set; }
    public string? DropboxAccessToken { get; set; }
    public string? DropboxRefreshToken { get; set; }
    [NotMapped]
    public bool IsDropboxSource => !string.IsNullOrEmpty(DropboxAccessToken) && !string.IsNullOrEmpty(DropboxRefreshToken);
}