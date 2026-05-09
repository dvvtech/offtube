using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Options;
using Offtube.Api.Configuration;
using Offtube.Api.Services.Abstract;

namespace Offtube.Api.Services
{
    public class S3StorageService : IStorageService
    {
        private readonly IAmazonS3 _s3Client;

        private readonly string _bucketName;

        public S3StorageService(IOptions<S3CloudConfig> settings)
        {
            var config = settings.Value;
            _bucketName = config.BucketName;

            // Настройка клиента для S3-совместимых хранилищ
            var s3Config = new AmazonS3Config
            {
                // Используем кастомный эндпоинт (DigitalOcean, MinIO, VK, etc.)
                ServiceURL = config.Endpoint,
                ForcePathStyle = true,
                // Указываем регион, даже если он не используется провайдером,
                // SDK требует его для подписи запросов (Signature V4)
                AuthenticationRegion = config.Region ?? "us-east-1"
            };

            var fullAccessKey = $"{config.TenantId}:{config.AccessKey}";

            var credentials = new BasicAWSCredentials(fullAccessKey, config.SecretKey);
            _s3Client = new AmazonS3Client(credentials, s3Config);
        }

        /// <summary>
        /// Загрузка файла с диска (FileInfo)
        /// </summary>
        public async Task<string> UploadFileAsync(
            FileInfo fileInfo,
            string objectKey = null,
            CancellationToken cancellationToken = default)
        {
            if (fileInfo == null || !fileInfo.Exists)
                throw new ArgumentException("Файл не существует.");

            var key = string.IsNullOrEmpty(objectKey)
                ? fileInfo.Name
                : objectKey;

            using var transferUtility = new TransferUtility(_s3Client);

            await using var stream = fileInfo.OpenRead();

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                BucketName = _bucketName,
                Key = key
            };

            try
            {
                await transferUtility.UploadAsync(uploadRequest, cancellationToken);

                return key;
            }
            catch (AmazonS3Exception ex)
            {
                throw new Exception(
                    $"Ошибка S3: {ex.Message} (Код: {ex.ErrorCode})",
                    ex);
            }
        }

        public async Task<string> GetPresignedUrlAsync(string objectKey, double expiresHours = 1)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                Expires = DateTime.UtcNow.AddHours(expiresHours)
            };

            return _s3Client.GetPreSignedURL(request);
        }
    }
}
