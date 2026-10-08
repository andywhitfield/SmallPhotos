namespace SmallPhotos.Service.BackgroundServices;

public class AlbumChangeServiceOptions
{
    public TimeSpan PollPeriod { get; set; } = TimeSpan.FromDays(1);
    public bool Enabled { get; set; } = true;
    public int SyncPhotoBatchSize { get; set; } = 5;
}