using Amazon.CloudWatchLogs.Model;

namespace Tender_Tool_Logs_Lambda.Interfaces
{
    public interface ILogFormatterService
    {
        /// <summary>
        /// Formats a list of log events into a single HTML report string.
        /// </summary>
        /// <param name="functionName">The friendly name of the function.</param>
        /// <param name="category">The category of the function.</param>
        /// <param name="logEvents">The list of log events.</param>
        /// <returns>A string containing the formatted HTML report.</returns>
        string FormatLogsAsText(string functionName, string category, List<OutputLogEvent> logEvents);
    }
}
