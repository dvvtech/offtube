//using System;
//using System.Diagnostics;
//using System.IO;
//using System.Threading.Tasks;

//namespace YtDlpDownloader
//{
//    class Program
//    {
//        private const string PROXY_URL = "http://vova01:1q2w3e$$$$@194.156.103.12:1300";
//        private const bool USE_PROXY_ENV_VARIABLES = true; // Использовать переменные окружения
//        private const bool VERBOSE_OUTPUT = true; // Подробный вывод

//        static async Task Main(string[] args)
//        {
//            Console.WriteLine("=== YouTube Video Downloader ===");

//            // Проверяем наличие yt-dlp
//            if (!File.Exists("yt-dlp.exe") && !CheckIfYtDlpInPath())
//            {
//                Console.WriteLine("yt-dlp не найден!");
//                Console.WriteLine("Скачайте yt-dlp с https://github.com/yt-dlp/yt-dlp/releases");
//                Console.WriteLine("и поместите в папку с программой или добавьте в PATH");
//                return;
//            }

//            try
//            {
//                // Пример использования
//                string url = "https://www.youtube.com/watch?v=9yXY9uMB_yE";//GetVideoUrlFromUser();
//                string downloadPath = "vid";//GetDownloadPath();
//                string quality = "best[height <= 480]";//GetQualityPreference();

//                await DownloadVideoAsync(url, downloadPath, quality);

//                Console.WriteLine("\nЗагрузка завершена!");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Ошибка: {ex.Message}");
//            }

//            Console.WriteLine("\nНажмите любую клавишу для выхода...");
//            Console.ReadKey();
//        }

//        static string GetVideoUrlFromUser()
//        {
//            Console.Write("\nВведите URL видео: ");
//            return Console.ReadLine();
//        }

//        static string GetDownloadPath()
//        {
//            Console.Write("Введите путь для сохранения (Enter для текущей папки): ");
//            string path = Console.ReadLine();
//            return string.IsNullOrWhiteSpace(path) ? Directory.GetCurrentDirectory() : path;
//        }

//        static string GetQualityPreference()
//        {
//            Console.WriteLine("\nВыберите качество:");
//            Console.WriteLine("1. Лучшее (1080p или выше)");
//            Console.WriteLine("2. Хорошее (720p)");
//            Console.WriteLine("3. Экономное (480p)");
//            Console.WriteLine("4. Только аудио");
//            Console.Write("Ваш выбор (1-4): ");

//            return Console.ReadLine() switch
//            {
//                "1" => "best",
//                "2" => "best[height<=720]",
//                "3" => "best[height<=480]",
//                "4" => "bestaudio",
//                _ => "best"
//            };
//        }

//        static async Task DownloadVideoAsync(string url, string outputPath, string quality)
//        {
//            string ytDlpPath = GetYtDlpPath();

//            // Создаем аргументы для yt-dlp
//            string arguments = BuildArguments(url, outputPath, quality);

//            Console.WriteLine($"\nВыполняется команда: yt-dlp {arguments}");

//            using var process = new Process();
//            process.StartInfo = new ProcessStartInfo
//            {
//                FileName = ytDlpPath,
//                Arguments = arguments,
//                UseShellExecute = false,
//                RedirectStandardOutput = true,
//                RedirectStandardError = true,
//                CreateNoWindow = false,
//                WorkingDirectory = outputPath
//            };

//            ConfigureProxyEnvironmentVariables(process.StartInfo.EnvironmentVariables);

//            process.Start();

//            // Асинхронное чтение вывода
//            var outputTask = Task.Run(() =>
//            {
//                while (!process.StandardOutput.EndOfStream)
//                {
//                    string line = process.StandardOutput.ReadLine();
//                    if (!string.IsNullOrEmpty(line))
//                        Console.WriteLine(line);
//                }
//            });

//            var errorTask = Task.Run(() =>
//            {
//                while (!process.StandardError.EndOfStream)
//                {
//                    string line = process.StandardError.ReadLine();
//                    if (!string.IsNullOrEmpty(line))
//                        Console.WriteLine($"Ошибка: {line}");
//                }
//            });

//            await Task.WhenAll(outputTask, errorTask);
//            await process.WaitForExitAsync();
//        }

//        static void ConfigureProxyEnvironmentVariables(System.Collections.Specialized.StringDictionary env)
//        {
//            // Устанавливаем переменные окружения для прокси
//            if (PROXY_URL.StartsWith("http://"))
//            {
//                env["HTTP_PROXY"] = PROXY_URL;
//                env["HTTPS_PROXY"] = PROXY_URL.Replace("http://", "https://");
//                env["http_proxy"] = PROXY_URL.ToLower();
//                env["https_proxy"] = PROXY_URL.Replace("http://", "https://").ToLower();
//            }
//            else if (PROXY_URL.StartsWith("https://"))
//            {
//                env["HTTP_PROXY"] = PROXY_URL.Replace("https://", "http://");
//                env["HTTPS_PROXY"] = PROXY_URL;
//                env["http_proxy"] = PROXY_URL.Replace("https://", "http://").ToLower();
//                env["https_proxy"] = PROXY_URL.ToLower();
//            }
//            else if (PROXY_URL.StartsWith("socks"))
//            {
//                // SOCKS прокси
//                env["ALL_PROXY"] = PROXY_URL;
//                env["all_proxy"] = PROXY_URL.ToLower();
//            }
//        }

//        static string BuildArguments(string url, string outputPath, string quality)
//        {
//            // Базовые параметры
//            var args = $"-o \"%(title)s.%(ext)s\" -f \"{quality}\" ";

//            // Дополнительные параметры
//            args += "--no-playlist "; // Не скачивать плейлист, только одно видео
//            args += "--progress ";    // Показывать прогресс
//            args += "--no-warnings "; // Скрыть предупреждения

//            // Если выбрано только аудио
//            if (quality == "bestaudio")
//            {
//                args += "-x --audio-format mp3 "; // Конвертировать в mp3
//            }

//            args += $"\"{url}\"";

//            return args;
//        }

//        static string GetYtDlpPath()
//        {
//            // Проверяем в текущей папке
//            if (File.Exists("yt-dlp.exe"))
//                return "yt-dlp.exe";

//            if (File.Exists("yt-dlp"))
//                return "yt-dlp";

//            // Если не найден локально, предполагаем что в PATH
//            return "yt-dlp";
//        }

//        static bool CheckIfYtDlpInPath()
//        {
//            try
//            {
//                using var process = new Process();
//                process.StartInfo = new ProcessStartInfo
//                {
//                    FileName = "yt-dlp",
//                    Arguments = "--version",
//                    UseShellExecute = false,
//                    RedirectStandardOutput = true,
//                    CreateNoWindow = true
//                };

//                process.Start();
//                process.WaitForExit(1000);
//                return process.ExitCode == 0;
//            }
//            catch
//            {
//                return false;
//            }
//        }
//    }
//}