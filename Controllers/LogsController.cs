using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tender_Tool_Logs_Lambda.Interfaces;
using Tender_Tool_Logs_Lambda.Models;

namespace Tender_Tool_Logs_Lambda.Controllers
{
    [ApiController]
    [Route("api/logs")]
    public class LogsController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogMapperService _logMapperService;
        private readonly ICloudWatchService _cloudWatchService;
        private readonly IPdfService _pdfService;
        private readonly IS3Service _s3Service;
        private readonly ILogger<LogsController> _logger;
        private readonly string _s3BucketName;

        public LogsController(
            IAuthService authService,
            ILogMapperService logMapperService,
            ICloudWatchService cloudWatchService,
            IPdfService pdfService,
            IS3Service s3Service,
            ILogger<LogsController> logger,
            IConfiguration configuration)
        {
            _authService = authService;
            _logMapperService = logMapperService;
            _cloudWatchService = cloudWatchService;
            _pdfService = pdfService;
            _s3Service = s3Service;
            _logger = logger;

            // Get the S3 bucket name from configuration (e.g., appsettings.json)
            _s3BucketName = configuration["S3_BUCKET_NAME"];
            if (string.IsNullOrEmpty(_s3BucketName))
            {
                _logger.LogError("S3_BUCKET_NAME is not configured.");
                // This will cause the service to fail fast if config is missing
                throw new InvalidOperationException("S3 bucket name is not configured.");
            }
        }

        /// <summary>
        /// Generates a PDF log report from CloudWatch and returns a secure S3 download link.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GenerateLogReport([FromBody] LogRequest request)
        {
            try
            {
                _logger.LogInformation("Log report request received for {Category} -> {Function} by User {UserId}",
                    request.Category, request.FunctionName, request.UserId);

                // 1. Authenticate the user
                _logger.LogDebug("Step 1: Authenticating user.");
                if (!await _authService.IsSuperUserAsync(request.UserId))
                {
                    _logger.LogWarning("Unauthorized attempt by User {UserId}", request.UserId);
                    return Unauthorized(new { Message = "User is not authorized to perform this action." });
                }

                // 2. Map friendly names to the real log group name
                _logger.LogDebug("Step 2: Mapping log group name.");
                string? logGroupName = _logMapperService.GetLogGroupName(request.Category, request.FunctionName);
                if (logGroupName == null)
                {
                    _logger.LogWarning("Log group mapping not found for {Category} -> {Function}", request.Category, request.FunctionName);
                    return NotFound(new { Message = $"Log group mapping not found for '{request.Category}' -> '{request.FunctionName}'." });
                }
                _logger.LogInformation("Mapped to log group: {LogGroup}", logGroupName);

                // 3. Get log events from CloudWatch
                _logger.LogDebug("Step 3: Fetching logs from CloudWatch.");
                var logEvents = await _cloudWatchService.GetLatestLogEventsAsync(logGroupName);
                _logger.LogInformation("Retrieved {Count} log events.", logEvents.Count);

                // 4. Generate the PDF
                _logger.LogDebug("Step 4: Generating PDF report.");
                string pdfTitle = $"Log Report: {request.FunctionName} ({request.Category})";
                byte[] pdfBytes = _pdfService.GenerateLogPdf(pdfTitle, logEvents);
                _logger.LogInformation("PDF report generated ({Bytes} bytes).", pdfBytes.Length);

                // 5. Upload PDF to S3
                _logger.LogDebug("Step 5: Uploading PDF to S3 bucket {BucketName}.", _s3BucketName);
                string fileKey = $"log-reports/{request.FunctionName}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.pdf";

                await _s3Service.UploadFileAsync(_s3BucketName, fileKey, pdfBytes, "application/pdf");
                _logger.LogInformation("Successfully uploaded to S3: {FileKey}", fileKey);

                // 6. Get a pre-signed URL for the file
                _logger.LogDebug("Step 6: Generating pre-signed URL.");
                string downloadUrl = await _s3Service.GetPreSignedUrlAsync(_s3BucketName, fileKey);

                // 7. Return the successful response
                _logger.LogInformation("Step 7: Returning successful response.");
                var response = new LogResponse
                {
                    FileName = fileKey,
                    DownloadUrl = downloadUrl
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled error occurred during log report generation for {Function}", request.FunctionName);
                return StatusCode(500, new { Message = "An internal server error occurred. Please check the logs." });
            }
        }
    }
}
