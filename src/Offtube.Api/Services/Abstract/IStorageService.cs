namespace Offtube.Api.Services.Abstract
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(FileInfo fileInfo, string objectKey = null, CancellationToken cancellationToken = default);
        Task<string> GetPresignedUrlAsync(string objectKey, double expiresHours = 1);
    }
}
