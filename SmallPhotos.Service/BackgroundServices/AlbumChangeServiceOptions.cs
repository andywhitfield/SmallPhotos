using System;

namespace SmallPhotos.Service.BackgroundServices
{
    public class AlbumChangeServiceOptions
    {
        public TimeSpan PollPeriod { get; set; } = TimeSpan.FromDays(1);
    }
}