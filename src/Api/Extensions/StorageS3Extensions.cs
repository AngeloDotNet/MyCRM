using Amazon.S3;

namespace Api.Extensions;

public static class StorageS3Extensions
{
    // Extension helper to ensure bucket exists
    //public static async Task EnsureBucketExistsAsync(this IAmazonS3 s3, string bucket)
    public static async void EnsureBucketExistsAsync(this IAmazonS3 s3, string bucket)
    {
        //var exists = await s3.DoesS3BucketExistAsync(bucket);
        await s3.EnsureBucketExistsAsync(bucket);

        //if (!exists)
        //{
        //    await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        //}
    }
}
