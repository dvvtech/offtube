namespace Offtube.Api.Configuration
{
    public class S3CloudConfig
    {
        public const string SectionName = "S3CloudConfig";

        public string Endpoint { get; init; }

        public string TenantId { get; init; }
        public string AccessKey { get; init; }
        public string SecretKey { get; init; }
        public string BucketName { get; init; }
        public string Region { get; init; }
    }
}
