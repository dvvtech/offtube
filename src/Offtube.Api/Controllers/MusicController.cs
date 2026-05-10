using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Offtube.Api.Configuration;
using Offtube.Api.Models;
using Offtube.Api.Services.Abstract;

namespace Offtube.Api.Controllers
{
    [Route("music")]
    [ApiController]
    public class MusicController : ControllerBase
    {
        private readonly IYoutubeDownloadService _downloadService;
        private readonly IStorageService _storageService;
        private readonly IOptions<GoogleRecaptchaConfig> _recaptchaOptions;
        private readonly ILogger<MusicController> _logger;        

        public MusicController(
            IYoutubeDownloadService downloadService,
            IStorageService storageService,
            IOptions<GoogleRecaptchaConfig> recaptchaOptions,            
            ILogger<MusicController> logger)
        {
            _downloadService = downloadService;
            _storageService = storageService;
            _recaptchaOptions = recaptchaOptions;
            _logger = logger;
        }

        /// <summary>
        /// Скачивает файл по url и закачивает его на s3 и возвращает ключ к файлу на s3
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("upload-from-url")]
        public async Task<IActionResult> UploadFromUrl([FromBody] UrlRequest request)
        {
            var downloadId = Guid.NewGuid().ToString();
            var tempPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "youtube_downloads",
                downloadId);

            await ProcessDownloadAsync(request, tempPath);

            if (!Directory.Exists(tempPath))
            {
                _logger.LogWarning($"Directory does not exist: {tempPath}");
                return BadRequest();
            }

            var file = Directory.GetFiles(tempPath).FirstOrDefault();
            if (file == null) return BadRequest();

            var fileInfo = new FileInfo(file);

            var objectKey = await _storageService.UploadFileAsync(fileInfo);

            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }

            return Ok(new UploadResponse
            {
                ObjectKey = objectKey,
                TrackTitle = fileInfo.Name
            });
        }

        private async Task ProcessDownloadAsync(UrlRequest request, string tempPath)
        {            
            var progress = new Progress<ProgressInfo>(info =>
            {
                //эта строка справедлива только если разворачивать в контейнере линукс
                //info.Percentage = info.Percentage / 10;                
            });

            await _downloadService.DownloadVideoAsync(
                request.Url,
                "bestaudio",
                tempPath,
                progress,
                CancellationToken.None);            
        }
    }
}
