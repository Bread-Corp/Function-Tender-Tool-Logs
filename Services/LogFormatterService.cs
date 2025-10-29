using Amazon.CloudWatchLogs.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Tender_Tool_Logs_Lambda.Interfaces;

namespace Tender_Tool_Logs_Lambda.Services
{
    public class LogFormatterService : ILogFormatterService
    {
        /// <summary>
        /// Formats a list of log events into a single HTML report string.
        /// </summary>
        public string FormatLogsAsText(string functionName, string category, List<OutputLogEvent> logEvents)
        {
            var sb = new StringBuilder();
            var generationTime = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
            var logCount = logEvents.Count;

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"UTF-Example\">");
            sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine($"  <title>Log Report: {functionName}</title>");

            // --- Modern "Tailwind-Inspired" CSS with Font Hierarchy ---
            sb.AppendLine("  <style>");
            // Import two font families: Lato (for headings/text) and Roboto Mono (for data/timestamps)
            sb.AppendLine("    @import url('https://fonts.googleapis.com/css2?family=Lato:wght@400;700&family=Roboto+Mono:wght@400&display=swap');");

            sb.AppendLine("    :root { --color-bg: #1a1a2e; --color-bg-light: #2a2a3e; --color-bg-hover: #3a3a4e; --color-border: #444; --color-text: #e0e0e0; --color-text-dim: #9e9e9e; --color-primary: #007bff; --color-primary-dark: #0056b3; --color-error-bg: #5d2b2b; --color-error-text: #ffdddd; --color-warn-bg: #5d512b; --color-warn-text: #ffffdd; }");

            // Use 'Lato' as the default font for the whole page
            sb.AppendLine("    body { font-family: 'Lato', Arial, sans-serif; font-weight: 400; background-color: var(--color-bg); color: var(--color-text); margin: 0; padding: 20px; -webkit-font-smoothing: antialiased; -moz-osx-font-smoothing: grayscale; }");
            sb.AppendLine("    .container { max-width: 1400px; margin: 20px auto; padding: 24px; background-color: var(--color-bg-light); border-radius: 12px; box-shadow: 0 10px 25px rgba(0,0,0,0.5); border: 1px solid var(--color-border); }");

            // --- Header Styling ---
            sb.AppendLine("    .header { display: flex; align-items: center; gap: 15px; border-bottom: 2px solid var(--color-primary); padding-bottom: 15px; }");
            sb.AppendLine("    .header svg { width: 40px; height: 40px; fill: var(--color-primary); }");
            // Use the bold (700 weight) 'Lato' font for the main heading
            sb.AppendLine("    h1 { color: var(--color-primary); margin: 0; font-size: 2.25rem; font-weight: 700; }");

            // --- Metadata Styling ---
            sb.AppendLine("    .meta-info { display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 15px 25px; background-color: var(--color-bg); padding: 20px; border-radius: 8px; margin: 25px 0; }");
            sb.AppendLine("    .meta-item { font-size: 1.1em; font-family: 'Lato', sans-serif; }"); // Default 'Lato'
            sb.AppendLine("    .meta-item strong { color: #009bff; display: block; font-size: 0.9em; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 5px; font-weight: 700; }");

            // --- Table Styling ---
            sb.AppendLine("    .table-container { width: 100%; overflow-x: auto; }");
            sb.AppendLine("    table { border-collapse: collapse; width: 100%; margin-top: 20px; }");
            sb.AppendLine("    th, td { border: 1px solid var(--color-border); padding: 12px 15px; text-align: left; vertical-align: top; }");
            // Use bold (700 weight) 'Lato' for table headers
            sb.AppendLine("    th { background-color: var(--color-primary-dark); color: white; position: sticky; top: 0; font-size: 1.1em; font-weight: 700; }");
            sb.AppendLine("    tr:nth-child(even) { background-color: #31314a; }");
            sb.AppendLine("    tr:hover { background-color: var(--color-bg-hover); }");

            // --- Log Cell Styling (The Font Switch) ---
            // Use 'Roboto Mono' for timestamps - it's a monospace font, great for data
            sb.AppendLine("    td.timestamp { width: 220px; font-family: 'Roboto Mono', monospace; color: var(--color-text-dim); }");
            // Use 'Lato' for the log message itself
            sb.AppendLine("    td.message { white-space: pre-wrap; word-break: break-word; font-family: 'Lato', sans-serif; }");

            // Highlighting (no font changes needed here, just colors)
            sb.AppendLine("    .log-error { background-color: var(--color-error-bg); color: var(--color-error-text); }");
            sb.AppendLine("    .log-warning { background-color: var(--color-warn-bg); color: var(--color-warn-text); }");
            sb.AppendLine("    .log-error .timestamp { color: #ffb8b8; }");
            sb.AppendLine("    .log-warning .timestamp { color: #ffffb8; }");

            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("  <div class=\"container\">");

            // --- Header with Icon ---
            sb.AppendLine("    <div class=\"header\">");
            sb.AppendLine("      <svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\"><path d=\"M13 9h5.5L13 3.5V9M6 2h8l6 6v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2m4 9H8v2h2v-2m4 0h-2v2h2v-2m0 4h-2v2h2v-2m-4-4H8v2h2v-2Z\"/></svg>");
            sb.AppendLine("      <h1>Log Report</h1>");
            sb.AppendLine("    </div>");

            // --- Metadata Box ---
            sb.AppendLine("    <div class=\"meta-info\">");
            sb.AppendLine($"      <div class=\"meta-item\"><strong>Function:</strong> {WebUtility.HtmlEncode(functionName)}</div>");
            sb.AppendLine($"      <div class=\"meta-item\"><strong>Category:</strong> {WebUtility.HtmlEncode(category)}</div>");
            sb.AppendLine($"      <div class=\"meta-item\"><strong>Generated:</strong> {generationTime}</div>");
            sb.AppendLine($"      <div class=\"meta-item\"><strong>Events Found:</strong> {logCount}</div>");
            sb.AppendLine("    </div>");

            // --- Log Table ---
            sb.AppendLine("    <div class=\"table-container\">");
            sb.AppendLine("      <table>");
            sb.AppendLine("        <thead>");
            sb.AppendLine("          <tr><th>Timestamp (UTC)</th><th>Message</th></tr>");
            sb.AppendLine("        </thead>");
            sb.AppendLine("        <tbody>");

            if (!logEvents.Any())
            {
                sb.AppendLine("<tr><td colspan=\"2\" style=\"text-align: center; padding: 20px;\">No log events found for this stream.</td></tr>");
            }
            else
            {
                foreach (var ev in logEvents)
                {
                    string timestampString = ev.Timestamp.HasValue ?
                        $"{ev.Timestamp.Value:yyyy-MM-dd HH:mm:ss.fff}" : "---";

                    string message = ev.Message ?? "---";
                    string cssClass = "";

                    if (message.Contains("error", StringComparison.OrdinalIgnoreCase)) { cssClass = "log-error"; }
                    else if (message.Contains("warning", StringComparison.OrdinalIgnoreCase)) { cssClass = "log-warning"; }

                    string encodedMessage = WebUtility.HtmlEncode(message);

                    sb.AppendLine($"<tr class=\"{cssClass}\">");
                    sb.AppendLine($"  <td class=\"timestamp\">{timestampString}</td>");
                    sb.AppendLine($"  <td class=\"message\">{encodedMessage}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("      </tbody>");
            sb.AppendLine("    </table>");
            sb.AppendLine("  </div>"); // End .table-container
            sb.AppendLine("  </div>"); // End .container
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}
