namespace SmallPhotos.Service.BackgroundServices;

public class GeoLocationServiceOptions
{
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(30);
    public string ApiKey { get; set; } = "";
    public TimeSpan PollPeriod { get; set; } = TimeSpan.FromMinutes(30);
    public bool Enabled { get; set; } = true;
    public int PhotoUpdateLimitPerRun { get; set; } = 1000;
    public int RateLimit { get; set; } = 1;
    public TimeSpan RateLimitWindow { get; set; } = TimeSpan.FromMilliseconds(200);
}
