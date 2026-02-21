namespace Offtube.Api.Models
{
    public class DownloadRequest
    {
        public string Url { get; set; }

        public string Quality { get; set; } = "best[height <= 480]";

        public string DownloadId { get; set; }

        public string ConnectionId { get; set; }

        public string RecaptchaToken { get; set; }
    }
}
