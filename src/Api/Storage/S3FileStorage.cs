using Amazon.S3;
using Amazon.S3.Model;

namespace Api.Storage;

public class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 s3;
    private readonly string bucket;

    public S3FileStorage(IAmazonS3 s3, IConfiguration config)
    {
        this.s3 = s3;

        bucket = config["S3:Bucket"] ?? "mycrm-attachments";
        this.s3.EnsureBucketExistsAsync(bucket).GetAwaiter().GetResult(); // ensure bucket exists
    }

    public async Task<string> UploadAsync(IFormFile file, string relativePath)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = relativePath,
            InputStream = file.OpenReadStream(),
            ContentType = file.ContentType
        };

        await s3.PutObjectAsync(request);

        // return object key (clients should use presigned urls)
        return relativePath;
    }

    public async Task<Stream> DownloadAsync(string relativePath)
    {
        var res = await s3.GetObjectAsync(bucket, relativePath);
        var ms = new MemoryStream();

        await res.ResponseStream.CopyToAsync(ms);
        ms.Position = 0;

        return ms;
    }

    public async Task<bool> ExistsAsync(string relativePath)
    {
        try
        {
            var res = await s3.GetObjectMetadataAsync(bucket, relativePath);

            return true;
        }
        catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}