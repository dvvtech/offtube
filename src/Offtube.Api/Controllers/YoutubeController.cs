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
            _logger.LogInformation("start download");
            //var args = "yt-dlp https://www.youtube.com/watch?v=m1Dk0qMSDEg";
            //request.Url = "https://www.youtube.com/watch?v=m1Dk0qMSDEg";
            request.Url = "https://www.youtube.com/watch?v=uVOzD-GX0kM";
            request.Quality = "best[height <= 480]";
            
            var downloadId = Guid.NewGuid().ToString();            
            var tempPath = Path.Combine(Directory.GetCurrentDirectory(), "youtube_downloads", downloadId);

            var progress = new Progress<ProgressInfo>(async info =>
            {
                _logger.LogInformation($"send progress, progress: {info.Percentage}%");

                // Отправляем прогресс через SignalR
                await _hubContext.Clients.Group(downloadId)
                    .SendAsync("ReceiveProgress", info);
            });
            _logger.LogInformation("download2");
            try
            {
                await _downloadService.DownloadVideoAsync(
                    request.Url,
                    request.Quality,
                    tempPath,
                    progress,
                    HttpContext.RequestAborted);
                _logger.LogInformation("download3");
                // Ищем скачанный файл
                var files = Directory.GetFiles(tempPath);
                var file = files.FirstOrDefault();

                if (file == null)
                    return NotFound("Файл не найден");

                // Читаем файл в поток
                var memory = new MemoryStream();
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                // Отправляем информацию о завершении
                await _hubContext.Clients.Group(downloadId)
                    .SendAsync("DownloadComplete", new
                    {
                        FileName = Path.GetFileName(file),
                        FileSize = memory.Length
                    });

                // Удаляем временные файлы после отправки
                Response.OnCompleted(async () =>
                {
                    await Task.Delay(1000); // Даем время на скачивание
                    Directory.Delete(tempPath, true);
                });

                var contentType = Path.GetExtension(file).ToLower() switch
                {
                    ".mp4" => "video/mp4",
                    ".mp3" => "audio/mpeg",
                    ".webm" => "video/webm",
                    _ => "application/octet-stream"
                };

                return File(memory, contentType, Path.GetFileName(file));
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError("Запрос отменен клиентом");
                return StatusCode(499, "Запрос отменен клиентом");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка");
                return StatusCode(500, $"Ошибка: {ex.Message}");
            }
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
