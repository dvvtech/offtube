using System;
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
                string url = "https://www.youtube.com/watch?v=xPIjetL93Ac";
                string downloadPath = "vid";
                string quality = "best[height<=480]";
                //string quality = "best[height<=720][fps<=60]";
                //string quality = "398+140";
                //string quality = "bestaudio";

                //await ListFormatsAsync(url);

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
            string ytDlpPath = "C:\\DVV\\Github\\Offtube\\src\\Offtube.Api\\Tools\\yt-dlp.exe";

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
                    {
                        Console.WriteLine(line);
                        ParseProgress(line);
                    }
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
            //string ffmpegPath = @"C:\DVV\Utils\ffmpeg-master-latest-linux64-gpl\bin";

            // Базовые параметры
            var args = $"-o \"%(title)s.%(ext)s\" -f \"{quality}\" ";

            //args += $"--ffmpeg-location \"{ffmpegPath}\" ";

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

        static async Task ListFormatsAsync(string url)
        {
            string ytDlpPath = "C:\\DVV\\Github\\Offtube\\src\\Offtube.Api\\Tools\\yt-dlp.exe";
            string arguments = $"--proxy \"{PROXY_URL}\" -F \"{url}\"";

            Console.WriteLine("Доступные форматы:");
            Console.WriteLine("==================");

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            };            

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            Console.WriteLine(output);
            if (!string.IsNullOrEmpty(error))
                Console.WriteLine($"Ошибка: {error}");
        }

        private static void ParseProgress(string line)
        {
            

            // Пример парсинга: [download]   0.0% of ~10.23MiB at 0B/s ETA Unknown
            if (line.Contains("[download]") && line.Contains("%"))
            {
                var percentMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d+\.?\d*)%");
                if (percentMatch.Success)
                {
                    var Percentage = (int)double.Parse(percentMatch.Groups[1].Value.Replace(".", ","));
                    //progressInfo.Status = "Загрузка...";
                }

                // Скорость
                var speedMatch = System.Text.RegularExpressions.Regex.Match(line, @"at\s+([\d\.]+\w+/s)");
                if (speedMatch.Success)
                {
                    var Speed = speedMatch.Groups[1].Value;
                }

                // ETA
                //var etaMatch = System.Text.RegularExpressions.Regex.Match(line, @"ETA\s+(\d+:\d+)");
                //if (etaMatch.Success)
                //    progressInfo.Eta = etaMatch.Groups[1].Value;
            }
            // Название файла
            //else if (line.Contains("[download] Destination:"))
            //{
            //    progressInfo.FileName = line.Replace("[download] Destination:", "").Trim();
            //    progressInfo.Status = "Начало загрузки...";
            //}
            //else if (line.Contains("[ExtractAudio] Destination:"))
            //{
            //    progressInfo.FileName = line.Replace("[ExtractAudio] Destination:", "").Trim();
            //    progressInfo.Status = "Конвертация...";
            //}

            //progress.Report(progressInfo);
        }
    }
}