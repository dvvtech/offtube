using Microsoft.AspNetCore.Mvc;
using Offtube.Api.Models;
using System.Diagnostics;

namespace Offtube.Api.Controllers
{
    [Route("api/youtube")]
    [ApiController]
    public class YoutubeController : ControllerBase
    {
        private readonly ILogger<YoutubeController> _logger;

        public YoutubeController(ILogger<YoutubeController> logger)
        {
            _logger = logger;
        }

        [HttpPost("download")]
        public async Task<IActionResult> Download([FromBody] DownloadRequest request)
        {
            var args = "yt-dlp https://www.youtube.com/watch?v=m1Dk0qMSDEg";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var stdoutTask = ReadStream(process.StandardOutput);
            var stderrTask = ReadStream(process.StandardError);

            await Task.WhenAll(stdoutTask, stderrTask);
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception("yt-dlp failed");
            }

            return Ok(123);
        }

        private async Task ReadStream(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line))
                    Console.WriteLine(line);
            }
        }
    }
}
