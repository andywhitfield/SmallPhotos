namespace SmallPhotos.Web.Model
{
    public class AlbumSourceFolderModel
    {
        public AlbumSourceFolderModel(int albumSourceId, string folder)
        {
            AlbumSourceId = albumSourceId;
            Folder = folder;
        }

        public int AlbumSourceId { get; }
        public string Folder { get; }
    }
}