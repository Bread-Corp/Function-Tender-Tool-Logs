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
        /// Fetches the latest log events from the latest log stream for a given log group.
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

            // 2. Get the *last* log events from that stream to avoid API Gateway's 29s timeout.
            var eventsRequest = new GetLogEventsRequest
            {
                LogGroupName = logGroupName,
                LogStreamName = latestStream.LogStreamName,
                StartFromHead = false, // Read from the TAIL (end) of the stream
                Limit = 200            // Only fetch a max of 200 events
            };

            var allEvents = new List<OutputLogEvent>();
            string? nextToken = null;

            // This loop will now be very fast and likely only run once.
            do
            {
                eventsRequest.NextToken = nextToken;
                var eventsResponse = await _cwClient.GetLogEventsAsync(eventsRequest);

                // When reading from the tail, events come in reverse chronological order.
                // We add them to our list and will sort them at the end.
                allEvents.AddRange(eventsResponse.Events);

                // Use NextBackwardToken when StartFromHead is false
                nextToken = eventsResponse.NextBackwardToken;

            } while (!string.IsNullOrEmpty(nextToken));

            _logger.LogInformation("Retrieved {Count} log events from stream {StreamName}.", allEvents.Count, latestStream.LogStreamName);

            // Because we read from the end, we must sort the final list
            // by timestamp to ensure the PDF is in the correct (chronological) order.
            return allEvents.OrderBy(e => e.Timestamp.GetValueOrDefault()).ToList();
        }
    }
}