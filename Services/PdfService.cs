using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatchLogs.Model;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Tender_Tool_Logs_Lambda.Interfaces;

namespace Tender_Tool_Logs_Lambda.Services
{
    public class PdfService : IPdfService
    {
        /// <summary>
        /// Generates a PDF document from a list of log events.
        /// </summary>
        /// <param name="title">The title to be displayed on the PDF header (e.g., "Logs for eTenderLambda").</param>
        /// <param name="logEvents">The list of log events to include in the report.</param>
        /// <returns>A byte array representing the PDF file.</returns>
        public byte[] GenerateLogPdf(string title, List<OutputLogEvent> logEvents)
        {
            // Generate the PDF document in memory and return it as a byte array.
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Basic page setup
                    page.Size(PageSizes.A4); // Standard A4 page
                    page.Margin(1, Unit.Centimetre); // 1cm margin
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // --- Header ---
                    page.Header().Element(header =>
                    {
                        header.Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(title).Bold().FontSize(16);
                                col.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                            });

                            row.RelativeItem().AlignRight().Text($"Total Events: {logEvents.Count}");
                        });

                        header.PaddingBottom(5, Unit.Millimetre);
                        header.BorderBottom(1, Unit.Point); // A thin line under the header
                    });

                    // --- Content ---
                    page.Content().Element(body =>
                    {
                        // Use a table to display the logs
                        body.PaddingVertical(5, Unit.Millimetre).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                // A fixed-width column for the timestamp
                                columns.ConstantColumn(120);
                                // A flexible column for the log message
                                columns.RelativeColumn();
                            });

                            // --- Table Header ---
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Timestamp (UTC)").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(2).Text("Message").Bold();
                            });

                            // --- Table Rows ---
                            if (!logEvents.Any())
                            {
                                // Show a message if there are no logs
                                table.Cell().ColumnSpan(2).Padding(10).AlignCenter().Text("No log events found for this stream.");
                            }
                            else
                            {
                                // Loop through each log event and add it as a row
                                foreach (var ev in logEvents)
                                {

                                    string timestampString; // Hold the formatted timestamp

                                    if (ev.Timestamp.HasValue)
                                    {
                                        // If it has a value, format it
                                        DateTime timestamp = ev.Timestamp.Value;
                                        timestampString = $"{timestamp:yyyy-MM-dd HH:mm:ss.fff}";
                                    }
                                    else
                                    {
                                        // If timestamp is null, use a placeholder
                                        timestampString = "---";
                                    }

                                    // Add the timestamp cell
                                    table.Cell().BorderBottom(0.5f, Unit.Point).Padding(2).Text(timestampString);
                                    // Add the message cell
                                    table.Cell().BorderBottom(0.5f, Unit.Point).Padding(2).Text(ev.Message ?? "---");
                                }
                            }
                        });
                    });

                    // --- Footer ---
                    page.Footer().AlignCenter().Text(text =>
                    {
                        // Add page numbers
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf(); // This returns the byte[]
        }
    }
}