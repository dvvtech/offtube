namespace Offtube.Api.Models
{
    public class ProgressInfo
    {
        public int Percentage { get; set; }
        public string Status { get; set; }
        public string FileName { get; set; }
        public double Speed { get; set; }
        public string Eta { get; set; }
    }
}
