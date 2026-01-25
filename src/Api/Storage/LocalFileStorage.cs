namespace Api.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly IWebHostEnvironment env;
    private readonly string basePath;

    public LocalFileStorage(IWebHostEnvironment env)
    {
        this.env = env;
        basePath = Path.Combine(this.env.WebRootPath ?? "wwwroot", "uploads");
        Directory.CreateDirectory(basePath);
    }

    public async Task<string> UploadAsync(IFormFile file, string relativePath)
    {
        var filePath = Path.Combine(basePath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        using var stream = new FileStream(filePath, FileMode.Create);

        await file.CopyToAsync(stream);
        return $"/uploads/{relativePath.Replace("\\\\", "/")}";
    }

    public Task<Stream> DownloadAsync(string relativePath)
    {
        var filePath = Path.Combine(basePath, relativePath);
        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string relativePath)
    {
        var filePath = Path.Combine(basePath, relativePath);

        return Task.FromResult(File.Exists(filePath));
    }
}