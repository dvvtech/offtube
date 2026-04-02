using Offtube.Api.Services.Abstract;

namespace Offtube.Api.Services
{
    public class AnalyticsTrackingService : IAnalyticsTrackingService
    {
        private const string TrackUrl = "http://analytics_api:8080/v1/analytics/track-offtube-tech";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AnalyticsTrackingService> _logger;

        public AnalyticsTrackingService(
            IHttpClientFactory httpClientFactory,
            ILogger<AnalyticsTrackingService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task TrackVisitAsync(
            string url,
            string clientIp,
            string? userAgent,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();

                using var request = new HttpRequestMessage(HttpMethod.Get, TrackUrl);
                request.Headers.Add("X-Forwarded-For", clientIp);
                request.Headers.Add("X-Real-IP", clientIp);
                request.Headers.Add("X-Media-Url", url);

                if (!string.IsNullOrWhiteSpace(userAgent))
                {
                    request.Headers.Add("User-Agent", userAgent);
                }

                using var response = await httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Analytics tracking failed with status code {StatusCode}",
                        response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track visit");
            }
        }
    }
}
