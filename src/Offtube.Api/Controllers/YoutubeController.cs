using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Offtube.Api.AppStart;
using Offtube.Api.Configuration;
using Offtube.Api.Hub;
using Offtube.Api.Models;
using Offtube.Api.Services.Abstract;
using System.Text.Json;

namespace Offtube.Api.Controllers
{
    [Route("video")]
    [ApiController]
    public class YoutubeController : ControllerBase
    {
        private readonly IAnalyticsTrackingService _analyticsTrackingService;
        private readonly IYoutubeDownloadService _downloadService;
        private readonly IHubContext<DownloadHub> _hubContext;
        private readonly ILogger<YoutubeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<GoogleRecaptchaConfig> _recaptchaOptions;

        public YoutubeController(
            IAnalyticsTrackingService analyticsTrackingService,
            IYoutubeDownloadService downloadService,
            IHubContext<DownloadHub> hubContext,
            IHttpClientFactory httpClientFactory,
            IOptions<GoogleRecaptchaConfig> recaptchaOptions,
            ILogger<YoutubeController> logger)
        {
            _analyticsTrackingService = analyticsTrackingService;
            _downloadService = downloadService;
            _hubContext = hubContext;
            _httpClientFactory = httpClientFactory;
            _recaptchaOptions = recaptchaOptions;
            _logger = logger;
        }

        [HttpPost("download")]
        public async Task<IActionResult> Download([FromBody] DownloadRequest request)
        {
            _logger.LogInformation($"quality: {request.Quality}");

            var clientIp = HttpContext.GetRealClientIp();
            var userAgent = Request.Headers["User-Agent"].ToString();

            _ = _analyticsTrackingService.TrackVisitAsync(request.Url, clientIp, userAgent);            

            var recaptchaValid = await ValidateRecaptcha(request.RecaptchaToken, _recaptchaOptions.Value.SecretKeyForOfftube);
            if (!recaptchaValid)
            {
                _logger.LogInformation("captcha not valid");
                return BadRequest("reCAPTCHA validation failed");
            }

            var downloadId = Guid.NewGuid().ToString();

            _ = Task.Run(async () =>
            {
                await ProcessDownloadAsync(request, downloadId);
            });                        

            return Accepted(new { downloadId });
        }

        private async Task ProcessDownloadAsync(DownloadRequest request, string downloadId)
        {
            var tempPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "youtube_downloads",
                downloadId);

            var progress = new Progress<ProgressInfo>(async info =>
            {
                //эта строка справедлива только если разворачивать в контейнере линукс
                info.Percentage = info.Percentage / 10;

                await _hubContext.Clients
                    .Client(request.ConnectionId)
                    .SendAsync("ReceiveProgress", info);                
            });

            await _downloadService.DownloadVideoAsync(
                request.Url,
                request.Quality,
                tempPath,
                progress,
                CancellationToken.None);

            var file = Directory.GetFiles(tempPath).FirstOrDefault();
            if (file == null) return;

            var fileInfo = new FileInfo(file);

            await _hubContext.Clients
                .Client(request.ConnectionId)                
                .SendAsync("DownloadComplete", new
                {
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length,
                    DownloadUrl = $"/video/file/{downloadId}"
                });

            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(10));

                var dir = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "youtube_downloads",
                    downloadId);

                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            });
        }

        [HttpGet("file/{downloadId}")]
        public IActionResult GetFile(string downloadId)
        {
            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "youtube_downloads",
                downloadId);

            var file = Directory.GetFiles(path).FirstOrDefault();
            if (file == null)
                return NotFound();

            var contentType = Path.GetExtension(file).ToLower() switch
            {
                ".mp4" => "video/mp4",
                ".mp3" => "audio/mpeg",
                ".webm" => "video/webm",
                _ => "application/octet-stream"
            };

            return PhysicalFile(file, contentType, Path.GetFileName(file), enableRangeProcessing: true);
        }        

        [HttpGet("test")]
        public async Task<string> Test()
        {
            //var downloadId = Guid.NewGuid().ToString();
            //var tempPath = Path.Combine(Directory.GetCurrentDirectory(), "youtube_downloads", downloadId);
            //Directory.CreateDirectory(tempPath);

            //var path = Path.Combine(Directory.GetCurrentDirectory(), "Tools", "yt-dlp");
            //var res = Directory.Exists(path);
            //var hasFile = System.IO.File.Exists(path);
            _logger.LogInformation("call test");            
            return "12345";
        }

        private async Task<bool> ValidateRecaptcha(string token, string secretKey)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var httpClient = _httpClientFactory.CreateClient();

            var response = await httpClient.GetStringAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}");

            var recaptchaResponse = JsonSerializer.Deserialize<RecaptchaResponse>(response);
            return recaptchaResponse?.Success == true && recaptchaResponse.Score >= 0.5;
        }
    }
}
