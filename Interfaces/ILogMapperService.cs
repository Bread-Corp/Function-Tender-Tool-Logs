namespace Tender_Tool_Logs_Lambda.Interfaces
{
    public interface ILogMapperService
    {
        /// <summary>
        /// Gets the full CloudWatch Log Group name based on the friendly category and function name.
        /// </summary>
        /// <param name="category">The function category (e.g., "scrapers").</param>
        /// <param name="functionName">The function friendly name (e.g., "eTenderLambda").</param>
        /// <returns>The full log group name (e.g., "/aws/lambda/eTendersLambda") or null if not found.</returns>
        string? GetLogGroupName(string category, string functionName);
    }
}
