using System.ComponentModel.DataAnnotations;

namespace Tender_Tool_Logs_Lambda.Models
{
    /// <summary>
    /// Represents the incoming request body for generating a log report.
    /// </summary>
    public class LogRequest
    {
        /// <summary>
        /// The category of the function (e.g., "scrapers", "pipeline").
        /// </summary>
        [Required]
        public string Category { get; set; }

        /// <summary>
        /// The friendly name of the Lambda function (e.g., "eTenderLambda").
        /// </summary>
        [Required]
        public string FunctionName { get; set; }

        /// <summary>
        /// The ID of the user making the request, for authentication.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }
    }
}
