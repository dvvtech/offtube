
using Microsoft.Extensions.Options;
using Offtube.Api.Configuration;
using Offtube.Api.Models;
using System.Diagnostics;
using System.Text;

namespace Offtube.Api.Services
{
    public class YoutubeDownloadService : IYoutubeDownloadService
    {        
        private readonly string _proxyUrl;
        private readonly string _ytDlpPath;
        
        private static readonly SemaphoreSlim _downloadLimiter = new SemaphoreSlim(3); // ← максимум 3 загрузки

        public YoutubeDownloadService(
            IOptions<AppConfig> options,
            IWebHostEnvironment env)
        {
            _proxyUrl = options.Value.ProxyUrl;

            if (env.IsDevelopment())
            {
                _ytDlpPath = Path.Combine(Directory.GetCurrentDirectory(), "Tools", "yt-dlp.exe");                
            }
            else
            {
                _ytDlpPath = Path.Combine(Directory.GetCurrentDirectory(), "Tools", "yt-dlp");
            }

            if (!File.Exists(_ytDlpPath))
            {
                throw new ArgumentException("yt-dlp not found");
            }
        }

        public async Task GetQualities(string mediaUrl)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ytDlpPath,
                    Arguments = @"-F " + mediaUrl,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };

            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            // Парсим output и извлекаем доступные качества
        }

        public async Task DownloadVideoAsync(
            string url,
            string quality,
            string outputPath,
            IProgress<ProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            await _downloadLimiter.WaitAsync(cancellationToken);
            //if (!_downloadLimiter.Wait(0))
            //{
            //    //throw new Exception("Сервер перегружен. Попробуйте позже.");
            //или
    //        await _hubContext.Clients
    //.Client(request.ConnectionId)
    //.SendAsync("Error", "Сервер перегружен. Попробуйте позже.");
            //}

            try
            {
                Directory.CreateDirectory(outputPath);

                var arguments = BuildArguments(url, outputPath, quality);

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _ytDlpPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        ParseProgress(e.Data, progress);
                };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        ParseProgress(e.Data, progress);
                };

                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                    throw new Exception("Ошибка при скачивании видео");
            }
            finally
            {
                _downloadLimiter.Release();
            }
        }

        private string BuildArguments(string url, string outputPath, string quality)
        {
            var outputTemplate = Path.Combine(outputPath, "%(title)s.%(ext)s");

            var args = $"-o \"{outputTemplate}\" ";
            args += $"-f \"{quality}\" ";
            args += "--no-playlist ";
            args += "--newline ";  // Для лучшего парсинга прогресса
            args += "--no-warnings ";
            args += $"--proxy \"{_proxyUrl}\" ";

            if (quality == "bestaudio")
                args += "-x --audio-format mp3 ";

            args += $"\"{url}\"";

            return args;
        }

        private void ParseProgress(string line, IProgress<ProgressInfo> progress)
        {
            // Парсинг прогресса из вывода yt-dlp
            var progressInfo = new ProgressInfo { Status = line };

            // Пример парсинга: [download]   0.0% of ~10.23MiB at 0B/s ETA Unknown
            if (line.Contains("[download]") && line.Contains("%"))
            {
                var percentMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d+\.?\d*)%");
                if (percentMatch.Success)
                {
                    progressInfo.Percentage = (int)double.Parse(percentMatch.Groups[1].Value.Replace(".",","));
                    progressInfo.Status = "Загрузка...";
                }

                // Скорость
                var speedMatch = System.Text.RegularExpressions.Regex.Match(line, @"at\s+([\d\.]+\w?/s)");
                if (speedMatch.Success)
                {
                    progressInfo.Speed =  double.Parse(speedMatch.Groups[1].Value);
                }

                // ETA
                var etaMatch = System.Text.RegularExpressions.Regex.Match(line, @"ETA\s+(\d+:\d+)");
                if (etaMatch.Success)
                    progressInfo.Eta = etaMatch.Groups[1].Value;
            }
            // Название файла
            else if (line.Contains("[download] Destination:"))
            {
                progressInfo.FileName = line.Replace("[download] Destination:", "").Trim();
                progressInfo.Status = "Начало загрузки...";
            }
            else if (line.Contains("[ExtractAudio] Destination:"))
            {
                progressInfo.FileName = line.Replace("[ExtractAudio] Destination:", "").Trim();
                progressInfo.Status = "Конвертация...";
            }

            progress.Report(progressInfo);
        }
    }
}
