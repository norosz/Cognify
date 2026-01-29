using System.Text;
using Cognify.Server.Services.Interfaces;
using UglyToad.PdfPig;

namespace Cognify.Server.Services;

public class PdfTextExtractor(ILogger<PdfTextExtractor> logger) : IPdfTextExtractor
{
    public Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));

        try
        {
            using var document = PdfDocument.Open(pdfStream);
            var builder = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var text = page.Text?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine().AppendLine("---").AppendLine();
                }

                builder.Append(text);
            }

            return Task.FromResult(builder.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to extract text from PDF.");
            throw;
        }
    }
}