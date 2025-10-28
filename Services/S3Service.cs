using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Tender_Tool_Logs_Lambda.Interfaces;

namespace Tender_Tool_Logs_Lambda.Services
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<S3Service> _logger;

        public S3Service(IAmazonS3 s3Client, ILogger<S3Service> logger)
        {
            _s3Client = s3Client;
            _logger = logger;
        }

        /// <summary>
        /// Uploads a file's content to an S3 bucket.
        /// </summary>
        /// <param name="bucketName">The name of the S3 bucket.</param>
        /// <param name="key">The full file name/key in the bucket (e.g., "reports/log.pdf").</param>
        /// <param name="content">The byte array content of the file.</param>
        /// <param name="contentType">The MIME type of the file (e.g., "application/pdf").</param>
        /// <returns>The S3 key of the uploaded object.</returns>
        public async Task<string> UploadFileAsync(string bucketName, string key, byte[] content, string contentType)
        {
            _logger.LogInformation("Uploading {Key} to bucket {BucketName}", key, bucketName);

            // Use a MemoryStream to upload the byte array
            using (var stream = new MemoryStream(content))
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    InputStream = stream,
                    ContentType = contentType,
                    CannedACL = S3CannedACL.Private // IMPORTANT: Keep the file private
                };

                await _s3Client.PutObjectAsync(putRequest);

                _logger.LogInformation("Successfully uploaded {Key}", key);
                return key;
            }
        }

        /// <summary>
        /// Generates a temporary, secure, pre-signed URL to download a private S3 object.
        /// </summary>
        /// <param name="bucketName">The name of the S3 bucket.</param>
        /// <param name="key">The key of the object to download.</param>
        /// <param name="durationInMinutes">How long the link should be valid for.</param>
        /// <returns>A string containing the pre-signed URL.</returns>
        public Task<string> GetPreSignedUrlAsync(string bucketName, string key, int durationInMinutes = 15)
        {
            _logger.LogInformation("Generating pre-signed URL for {Key} in {BucketName}", key, bucketName);

            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(durationInMinutes)
            };

            // GetPreSignedURL is a synchronous method in the SDK
            string url = _s3Client.GetPreSignedURL(request);

            // Wrap the synchronous result in a completed Task to match the interface
            return Task.FromResult(url);
        }
    }
}
