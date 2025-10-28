using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Microsoft.Extensions.Logging;
using Tender_Tool_Logs_Lambda.Interfaces;

namespace Tender_Tool_Logs_Lambda.Services
{
    public class CloudWatchService : ICloudWatchService
    {
        private readonly IAmazonCloudWatchLogs _cwClient;
        private readonly ILogger<CloudWatchService> _logger;

        public CloudWatchService(IAmazonCloudWatchLogs cwClient, ILogger<CloudWatchService> logger)
        {
            _cwClient = cwClient;
            _logger = logger;
        }

        /// <summary>
        /// Fetches all log events from the latest log stream for a given log group.
        /// </summary>
        /// <param name="logGroupName">The full name of the CloudWatch Log Group.</param>
        /// <returns>A list of <see cref="OutputLogEvent"/> objects.</returns>
        public async Task<List<OutputLogEvent>> GetLatestLogEventsAsync(string logGroupName)
        {
            // 1. Find the latest log stream
            var streamRequest = new DescribeLogStreamsRequest
            {
                LogGroupName = logGroupName,
                OrderBy = OrderBy.LastEventTime, // Order by the last event's timestamp
                Descending = true,               // Get the most recent one first
                Limit = 1                        // We only want the single latest stream
            };

            _logger.LogInformation("Finding latest log stream for {LogGroup}", logGroupName);

            DescribeLogStreamsResponse streamResponse;
            try
            {
                streamResponse = await _cwClient.DescribeLogStreamsAsync(streamRequest);
            }
            catch (ResourceNotFoundException ex)
            {
                _logger.LogWarning(ex, "Log group not found: {LogGroup}", logGroupName);
                return new List<OutputLogEvent>(); // Return empty list if log group doesn't exist
            }

            var latestStream = streamResponse.LogStreams.FirstOrDefault();

            if (latestStream == null)
            {
                _logger.LogWarning("No log streams found for {LogGroup}", logGroupName);
                return new List<OutputLogEvent>(); // Return empty list if no streams exist
            }

            _logger.LogInformation("Found stream: {StreamName}, Last event: {LastEvent}", latestStream.LogStreamName, latestStream.LastEventTimestamp);

            // 2. Get all log events from that specific stream
            var eventsRequest = new GetLogEventsRequest
            {
                LogGroupName = logGroupName,
                LogStreamName = latestStream.LogStreamName,
                StartFromHead = true // Get all events from the beginning of this stream
            };

            var allEvents = new List<OutputLogEvent>();
            string? nextToken = null;

            // Loop to handle pagination (if the stream has more than 1MB of logs)
            do
            {
                eventsRequest.NextToken = nextToken;
                var eventsResponse = await _cwClient.GetLogEventsAsync(eventsRequest);

                allEvents.AddRange(eventsResponse.Events);
                nextToken = eventsResponse.NextForwardToken;

            } while (!string.IsNullOrEmpty(nextToken));

            _logger.LogInformation("Retrieved {Count} log events from stream {StreamName}.", allEvents.Count, latestStream.LogStreamName);
            return allEvents;
        }
    }
}
