using Offtube.Api.Models;

namespace Offtube.Api.Services
{
    public interface IYoutubeDownloadService
    {
        Task DownloadVideoAsync(
            string url,
            string quality,
            string outputPath,
            IProgress<ProgressInfo> progress,
            CancellationToken cancellationToken);
    }
}
