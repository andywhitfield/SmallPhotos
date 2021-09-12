namespace SmallPhotos.Web.Model
{
    public class AlbumSourceFolderModel
    {
        public AlbumSourceFolderModel(long albumSourceId, string folder)
        {
            AlbumSourceId = albumSourceId;
            Folder = folder;
        }

        public long AlbumSourceId { get; }
        public string Folder { get; }
    }
}