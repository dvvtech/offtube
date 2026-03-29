namespace Offtube.Api.Configuration
{    
    public class ProxyConfig
    {
        public const string SectionName = "ProxySettings";
        public bool Enabled { get; init; }
        public string Ip { get; init; }
        public string Port { get; init; }
        public string Login { get; init; }
        public string Password { get; init; }
    }
}
