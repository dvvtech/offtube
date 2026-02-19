using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Offtube.Api.Hub;
using Offtube.Api.Models;
using Offtube.Api.Services;

namespace Offtube.Api.Controllers
{
    [Route("video")]
    [ApiController]
    public class YoutubeController : ControllerBase
    {
        private readonly IYoutubeDownloadService _downloadService;
        private readonly IHubContext<DownloadHub> _hubContext;
        private readonly ILogger<YoutubeController> _logger;

        public YoutubeController(
            IYoutubeDownloadService downloadService,
            IHubContext<DownloadHub> hubContext,
            ILogger<YoutubeController> logger)
        {
            _downloadService = downloadService;
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpPost("download")]
        public async Task<IActionResult> Download([FromBody] DownloadRequest request)
        {
            var downloadId = request.DownloadId;

            _ = Task.Run(async () =>
            {
                await ProcessDownloadAsync(request);
            });

            return Accepted(); // ← сразу ответ 202
        }

        private async Task ProcessDownloadAsync(DownloadRequest request)
        {
            var tempPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "youtube_downloads",
                request.DownloadId);

            var progress = new Progress<ProgressInfo>(async info =>
            {
                //эта строка справедлива только если разворачивать в контейнере линукс
                info.Percentage = info.Percentage / 10;

                await _hubContext.Clients
                    .Group(request.DownloadId)
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
                .Group(request.DownloadId)
                .SendAsync("DownloadComplete", new
                {
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length,
                    DownloadUrl = $"/video/file/{request.DownloadId}"
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

            return PhysicalFile(file, contentType, Path.GetFileName(file));
        }        

        [HttpGet("test")]
        public async Task<string> Test()
        {
            var downloadId = Guid.NewGuid().ToString();
            var tempPath = Path.Combine(Directory.GetCurrentDirectory(), "youtube_downloads", downloadId);
            Directory.CreateDirectory(tempPath);

            //var path = Path.Combine(Directory.GetCurrentDirectory(), "Tools", "yt-dlp");
            //var res = Directory.Exists(path);
            //var hasFile = System.IO.File.Exists(path);
            _logger.LogInformation("call test");            
            return "123";
        }
    }
}
