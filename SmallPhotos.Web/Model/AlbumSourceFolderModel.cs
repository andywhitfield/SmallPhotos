namespace SmallPhotos.Web.Model;

public class AlbumSourceFolderModel(long albumSourceId, string folder, bool recursive)
{
    public long AlbumSourceId { get; } = albumSourceId;
    public string Folder { get; } = folder;
    public bool Recursive { get; } = recursive;
}