using Microsoft.Extensions.Options;
using Offtube.Api.Configuration;
using Offtube.Api.Models;
using Offtube.Api.Services.Abstract;
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
            IOptions<ProxyConfig> options,
            IWebHostEnvironment env)
        {
            var config = options.Value;            
            _proxyUrl = $"http://{config.Login}:{config.Password}@{config.Ip}:{config.Port}";

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

        public async Task<string> GetVideoTitleAsync(string url)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ytDlpPath,
                    Arguments = $"--get-title --no-warnings --proxy \"{_proxyUrl}\" \"{url}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Error getting video title: {error}");
            }

            return output.Trim();
        }

        public async Task<List<VideoQuality>> GetQualities(string mediaUrl)
        {
            var qualities = new List<VideoQuality>();

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
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Ошибка при получении качеств: {error}");
            }

            // Парсим вывод
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                var quality = ParseQualityLine(line);
                if (quality != null)
                {
                    qualities.Add(quality);
                }
            }

            return qualities;
        }

        private VideoQuality ParseQualityLine(string line)
        {
            // Пример строки: "248 webm 1920x1080 1080 1 | ~31.27MiB 1490k https"
            var match = System.Text.RegularExpressions.Regex.Match(line,
                @"^(\d+)\s+(\w+)\s+(\d+x\d+)?\s*(\d+p)?");

            if (match.Success)
            {
                return new VideoQuality
                {
                    Id = match.Groups[1].Value,
                    Extension = match.Groups[2].Value,
                    Resolution = match.Groups[3].Value,
                    Quality = match.Groups[4].Value
                };
            }

            return null;
        }

        public async Task<string> GetBestFormatAsync(string url)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ytDlpPath,
                    Arguments = $"--list-formats --no-warnings --proxy \"{_proxyUrl}\" \"{url}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                return "best";

            var audioFormats = new List<(string id, string ext)>();

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!line.Contains("audio only", StringComparison.OrdinalIgnoreCase))
                    continue;

                var match = System.Text.RegularExpressions.Regex.Match(line.Trim(), @"^(\S+)\s+(\w+)");
                if (match.Success)
                {
                    audioFormats.Add((match.Groups[1].Value, match.Groups[2].Value.ToLowerInvariant()));
                }
            }
            
            if (audioFormats.Count == 0)
                return "best";

            static int ExtensionPriority(string ext) => ext switch
            {
                "m4a" => 0,
                "webm" => 1,
                _ => 2
            };

            static int FormatIdNumber(string id)
            {
                var numMatch = System.Text.RegularExpressions.Regex.Match(id, @"(\d+)$");
                return numMatch.Success ? int.Parse(numMatch.Groups[1].Value) : 0;
            }

            var best = audioFormats
                .OrderBy(f => ExtensionPriority(f.ext))
                .ThenByDescending(f => FormatIdNumber(f.id))
                .First();

            return best.id;
        }

        public async Task DownloadVideoAsync(
            string url,
            string quality,
            string outputPath,
            IProgress<ProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            await _downloadLimiter.WaitAsync(cancellationToken);
            
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
                {
                    var downloadedFiles = Directory.GetFiles(outputPath);
                    if (downloadedFiles.Length == 0)
                        throw new Exception("Ошибка при скачивании видео");
                }
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

            if (quality == "bestaudio" || quality == "bestaudio/best" || quality == "best")
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
                var speedMatch = System.Text.RegularExpressions.Regex.Match(line, @"at\s+([\d\.]+\w+/s)");
                if (speedMatch.Success)
                {
                    progressInfo.Speed =  speedMatch.Groups[1].Value;
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
