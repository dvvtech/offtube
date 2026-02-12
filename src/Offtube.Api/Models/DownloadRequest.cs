namespace Offtube.Api.Models
{
    public class DownloadRequest
    {
        public string Url { get; set; }

        public string Quality { get; set; } = "best[height <= 480]";
    }
}
