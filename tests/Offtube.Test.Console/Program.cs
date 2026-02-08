using System.Diagnostics;

namespace YtDlpDownloader
{
    class Program
    {
        private const string PROXY_URL = "http://vova01:1q2w3e$$$$@194.156.103.12:1300";

        static async Task Main(string[] args)
        {
            try
            {
                string url = "https://www.youtube.com/watch?v=9yXY9uMB_yE";
                string downloadPath = "vid";
                string quality = "best[height <= 480]";

                await DownloadVideoAsync(url, downloadPath, quality);

                Console.WriteLine("\nЗагрузка завершена!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        static async Task DownloadVideoAsync(string url, string outputPath, string quality)
        {
            string ytDlpPath = "yt-dlp.exe";

            // Создаем аргументы для yt-dlp
            string arguments = BuildArguments(url, outputPath, quality);

            Console.WriteLine($"\nВыполняется команда: yt-dlp {arguments}");

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false,
                WorkingDirectory = outputPath
            };

            //ConfigureProxyEnvironmentVariables(process.StartInfo.EnvironmentVariables);

            process.Start();

            // Асинхронное чтение вывода
            var outputTask = Task.Run(() =>
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                        Console.WriteLine(line);
                }
            });

            var errorTask = Task.Run(() =>
            {
                while (!process.StandardError.EndOfStream)
                {
                    string line = process.StandardError.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                        Console.WriteLine($"Ошибка: {line}");
                }
            });

            await Task.WhenAll(outputTask, errorTask);
            await process.WaitForExitAsync();
        }

        static void ConfigureProxyEnvironmentVariables(System.Collections.Specialized.StringDictionary env)
        {
            // Устанавливаем переменные окружения для прокси
            if (PROXY_URL.StartsWith("http://"))
            {
                env["HTTP_PROXY"] = PROXY_URL;
                env["HTTPS_PROXY"] = PROXY_URL.Replace("http://", "https://");
                env["http_proxy"] = PROXY_URL.ToLower();
                env["https_proxy"] = PROXY_URL.Replace("http://", "https://").ToLower();
            }
            else if (PROXY_URL.StartsWith("https://"))
            {
                env["HTTP_PROXY"] = PROXY_URL.Replace("https://", "http://");
                env["HTTPS_PROXY"] = PROXY_URL;
                env["http_proxy"] = PROXY_URL.Replace("https://", "http://").ToLower();
                env["https_proxy"] = PROXY_URL.ToLower();
            }
            else if (PROXY_URL.StartsWith("socks"))
            {
                // SOCKS прокси
                env["ALL_PROXY"] = PROXY_URL;
                env["all_proxy"] = PROXY_URL.ToLower();
            }
        }

        static string BuildArguments(string url, string outputPath, string quality)
        {
            // Базовые параметры
            var args = $"-o \"%(title)s.%(ext)s\" -f \"{quality}\" ";

            // Дополнительные параметры
            args += "--no-playlist "; // Не скачивать плейлист, только одно видео
            args += "--progress ";    // Показывать прогресс
            args += "--no-warnings "; // Скрыть предупреждения

            // Если выбрано только аудио
            if (quality == "bestaudio")
            {
                args += "-x --audio-format mp3 "; // Конвертировать в mp3
            }

            args += $"\"{url}\"";

            return args;
        }
    }
}