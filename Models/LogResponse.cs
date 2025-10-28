namespace Tender_Tool_Logs_Lambda.Models
{
    /// <summary>
    /// Represents the successful response containing the log report details.
    /// </summary>
    public class LogResponse
    {
        /// <summary>
        /// The name of the generated PDF file (e.g., "logs-eTenderLambda-202510281230.pdf").
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The secure, temporary S3 pre-signed URL to download the file.
        /// </summary>
        public string DownloadUrl { get; set; }
    }
}
