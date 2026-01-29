namespace Cognify.Server.Services.Interfaces;

public interface IPdfImageExtractor
{
    Task<List<PdfEmbeddedImage>> ExtractImagesAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}

public record PdfEmbeddedImage(
    byte[]? Bytes,
    string ContentType,
    int PageNumber,
    int Index,
    int? Width,
    int? Height
);