namespace Api.Storage;

public interface IFileStorage
{
    Task<string> UploadAsync(IFormFile file, string relativePath);
    Task<Stream> DownloadAsync(string relativePath);
    Task<bool> ExistsAsync(string relativePath);
}