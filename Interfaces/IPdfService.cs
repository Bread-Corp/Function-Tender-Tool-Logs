using Amazon.CloudWatchLogs.Model;

namespace Tender_Tool_Logs_Lambda.Interfaces
{
    public interface IPdfService
    {
        /// <summary>
        /// Generates a PDF document from a list of log events.
        /// </summary>
        /// <param name="title">The title to be displayed on the PDF header (e.g., "Logs for eTenderLambda").</param>
        /// <param name="logEvents">The list of log events to include in the report.</param>
        /// <returns>A byte array representing the PDF file.</returns>
        byte[] GenerateLogPdf(string title, List<OutputLogEvent> logEvents);
    }
}
