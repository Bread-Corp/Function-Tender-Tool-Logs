using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Tender_Tool_Logs_Lambda.Interfaces;

namespace Tender_Tool_Logs_Lambda.Services
{
    public class LogMapperService : ILogMapperService
    {
        private readonly ILogger<LogMapperService> _logger;

        // This dictionary holds all your mappings.
        // The key is a (category, functionName) tuple, and the value is the log group name.
        private readonly Dictionary<(string, string), string> _logGroupMappings;

        public LogMapperService(ILogger<LogMapperService> logger)
        {
            _logger = logger;

            // We initialize the map here using our new custom comparer
            // to make the (string, string) tuple key case-insensitive.
            _logGroupMappings = new Dictionary<(string, string), string>(new TupleCaseInsensitiveComparer())
            {
                // --- SCRAPERS AND CRAWLERS CATEGORY ---
                { ("scrapers", "eTenderLambda"), "/aws/lambda/eTendersLambda" },
                { ("scrapers", "EskomLambda"), "/aws/lambda/EskomLambda" },
                { ("scrapers", "TransnetLambda"), "/aws/lambda/TransnetLambda" },
                { ("scrapers", "SanralLambda"), "/aws/lambda/SanralFunction" },
                { ("scrapers", "SarsLambda"), "/aws/lambda/SarsLambda" },

                // --- DATA PIPELINE CATEGORY ---
                { ("pipeline", "DeduplicationLambda"), "/aws/lambda/TenderDeduplicationLambda" },
                { ("pipeline", "AISummaryLambda"), "/aws/lambda/AILambda" },
                { ("pipeline", "AITaggingLambda"), "/aws/lambda/TenderAITaggingLambda" },
                { ("pipeline", "DBWriterLambda"), "/aws/lambda/TenderDatabaseWriterLambda" },
                { ("pipeline", "TenderCleanupLambda"), "/aws/lambda/TenderCleanupHandler" }
            };
        }

        /// <summary>
        /// Gets the full CloudWatch Log Group name based on the friendly category and function name.
        /// </summary>
        /// <param name="category">The function category (e.g., "scrapers").</param>
        /// <param name="functionName">The function friendly name (e.g., "eTenderLambda").</param>
        /// <returns>The full log group name (e.g., "/aws/lambda/eTendersLambda") or null if not found.</returns>
        public string? GetLogGroupName(string category, string functionName)
        {
            if (_logGroupMappings.TryGetValue((category, functionName), out var logGroupName))
            {
                return logGroupName;
            }

            // If the mapping doesn't exist, log a warning and return null.
            _logger.LogWarning("No log group mapping found for Category: {Category}, Function: {Function}", category, functionName);
            return null;
        }

        // --- Private nested class for case-insensitive tuple comparison ---
        private class TupleCaseInsensitiveComparer : IEqualityComparer<(string, string)>
        {
            public bool Equals((string, string) x, (string, string) y)
            {
                // Check if both strings in the tuple are equal, ignoring case.
                return string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode([DisallowNull] (string, string) obj)
            {
                // Get case-insensitive hash codes for both strings
                int hash1 = (obj.Item1 != null) ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1) : 0;
                int hash2 = (obj.Item2 != null) ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2) : 0;

                // Combine the hash codes
                return HashCode.Combine(hash1, hash2);
            }
        }
    }
}