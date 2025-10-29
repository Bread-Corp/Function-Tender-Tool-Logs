using Amazon.CloudWatchLogs.Model;

namespace Tender_Tool_Logs_Lambda.Interfaces
{
    public interface ILogFormatterService
    {
        /// <summary>
        /// Formats a list of log events into a single plain text string.
        /// </summary>
        /// <param name="logEvents">The list of log events.</param>
        /// <returns>A string containing the formatted logs.</returns>
        string FormatLogsAsText(List<OutputLogEvent> logEvents);
    }
}
