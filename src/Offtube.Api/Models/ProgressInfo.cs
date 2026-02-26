namespace Offtube.Api.Models
{
    public class ProgressInfo
    {
        public int Percentage { get; set; }
        public string Status { get; set; }
        public string FileName { get; set; }
        public string Speed { get; set; }
        public string Eta { get; set; }
    }
}
