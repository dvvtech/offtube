using Offtube.Api.Models;

namespace Offtube.Api.Services.Abstract
{
    public interface IYoutubeDownloadService
    {
        Task<string> GetVideoTitleAsync(string url);

        Task<string> GetBestFormatAsync(string url);

        Task DownloadVideoAsync(
            string url,
            string quality,
            string outputPath,
            IProgress<ProgressInfo> progress,
            CancellationToken cancellationToken);
    }
}
