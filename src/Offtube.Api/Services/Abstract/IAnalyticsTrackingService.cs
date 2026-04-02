namespace Offtube.Api.Services.Abstract
{
    public interface IAnalyticsTrackingService
    {
        Task TrackVisitAsync(
            string url,
            string clientIp,
            string? userAgent,
            CancellationToken cancellationToken = default);
    }
}
