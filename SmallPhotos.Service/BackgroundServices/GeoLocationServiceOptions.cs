namespace SmallPhotos.Service.BackgroundServices;

public class GeoLocationServiceOptions
{
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(10);
    public string ApiKey { get; set; } = "";
    public TimeSpan PollPeriod { get; set; } = TimeSpan.FromDays(1);
    public bool Enabled { get; set; } = true;
    public int PhotoUpdateLimitPerRun { get; set; } = 23;
    public int RateLimit { get; set; } = 1;
    public TimeSpan RateLimitWindow { get; set; } = TimeSpan.FromMilliseconds(200);
}
