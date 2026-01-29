namespace Cognify.Server.Services.Interfaces;

public interface IPdfTextExtractor
{
    Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}