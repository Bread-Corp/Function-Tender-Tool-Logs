namespace Tender_Tool_Logs_Lambda.Interfaces
{
    public interface IS3Service
    {
        /// <summary>
        /// Uploads a file's content to an S3 bucket.
        /// </summary>
        /// <param name="bucketName">The name of the S3 bucket.</param>
        /// <param name="key">The full file name/key in the bucket (e.g., "reports/log.pdf").</param>
        /// <param name="content">The byte array content of the file.</param>
        /// <param name="contentType">The MIME type of the file (e.g., "application/pdf").</param>
        /// <returns>The S3 key of the uploaded object.</returns>
        Task<string> UploadFileAsync(string bucketName, string key, byte[] content, string contentType);

        /// <summary>
        /// Generates a temporary, secure, pre-signed URL to download a private S3 object.
        /// </summary>
        /// <param name="bucketName">The name of the S3 bucket.</param>
        /// <param name="key">The key of the object to download.</param>
        /// <param name="durationInMinutes">How long the link should be valid for.</param>
        /// <returns>A string containing the pre-signed URL.</returns>
        Task<string> GetPreSignedUrlAsync(string bucketName, string key, int durationInMinutes = 15);
    }
}
