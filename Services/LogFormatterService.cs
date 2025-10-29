using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.CloudWatchLogs.Model;
using Tender_Tool_Logs_Lambda.Interfaces;

namespace Tender_Tool_Logs_Lambda.Services
{
    public class LogFormatterService : ILogFormatterService
    {
        /// <summary>
        /// Formats a list of log events into a single plain text string.
        /// </summary>
        /// <param name="logEvents">The list of log events.</param>
        /// <returns>A string containing the formatted logs.</returns>
        public string FormatLogsAsText(List<OutputLogEvent> logEvents)
        {
            var sb = new StringBuilder();

            if (!logEvents.Any())
            {
                sb.AppendLine("No log events found for this stream.");
                return sb.ToString();
            }

            // Simple header for each log line
            sb.AppendLine("Timestamp (UTC)         | Message");
            sb.AppendLine("--------------------------|-----------------------------------------");

            foreach (var ev in logEvents)
            {
                string timestampString;
                if (ev.Timestamp.HasValue)
                {
                    DateTime timestamp = ev.Timestamp.Value;
                    timestampString = $"{timestamp:yyyy-MM-dd HH:mm:ss.fff}";
                }
                else
                {
                    timestampString = "---";
                }

                // Append the formatted line
                // PadRight ensures alignment for timestamps up to 26 chars
                sb.Append(timestampString.PadRight(26));
                sb.Append("| ");
                sb.AppendLine(ev.Message ?? "---");
            }

            return sb.ToString();
        }
    }
}
