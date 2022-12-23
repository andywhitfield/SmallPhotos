namespace SmallPhotos.Web.Model
{
    public class AlbumSourceFolderModel
    {
        public AlbumSourceFolderModel(long albumSourceId, string folder, bool recursive)
        {
            AlbumSourceId = albumSourceId;
            Folder = folder;
            Recursive = recursive;
        }

        public long AlbumSourceId { get; }
        public string Folder { get; }
        public bool Recursive { get; }
    }
}