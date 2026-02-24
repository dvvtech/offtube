using System.Diagnostics;

namespace YtDlpDownloader
{
    class Program
    {
        private const string PROXY_URL = "";

        static async Task Main(string[] args)
        {
            try
            {
                string url = "";
                string downloadPath = "vid";
                //string quality = "best[height <= 480]";
                string quality = "bestaudio";

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

        static string BuildArguments(string url, string outputPath, string quality)
        {
            string ffmpegPath = @"C:\DVV\Utils\ffmpeg-master-latest-linux64-gpl\bin";

            // Базовые параметры
            var args = $"-o \"%(title)s.%(ext)s\" -f \"{quality}\" ";

            args += $"--ffmpeg-location \"{ffmpegPath}\" ";

            // Дополнительные параметры
            args += "--no-playlist "; // Не скачивать плейлист, только одно видео
            args += "--progress ";    // Показывать прогресс
            args += "--no-warnings "; // Скрыть предупреждения

            args += $"--proxy \"{PROXY_URL}\" ";

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