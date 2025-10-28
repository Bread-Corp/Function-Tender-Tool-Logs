using Amazon.CloudWatchLogs.Model;

namespace Tender_Tool_Logs_Lambda.Interfaces
{
    public interface ICloudWatchService
    {
        /// <summary>
        /// Fetches all log events from the latest log stream for a given log group.
        /// </summary>
        /// <param name="logGroupName">The full name of the CloudWatch Log Group.</param>
        /// <returns>A list of <see cref="OutputLogEvent"/> objects.</returns>
        Task<List<OutputLogEvent>> GetLatestLogEventsAsync(string logGroupName);
    }
}
