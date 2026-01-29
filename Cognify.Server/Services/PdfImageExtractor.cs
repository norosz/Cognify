using System.Reflection;
using Cognify.Server.Services.Interfaces;
using UglyToad.PdfPig;

namespace Cognify.Server.Services;

public class PdfImageExtractor(ILogger<PdfImageExtractor> logger) : IPdfImageExtractor
{
    public Task<List<PdfEmbeddedImage>> ExtractImagesAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        if (pdfStream == null) throw new ArgumentNullException(nameof(pdfStream));

        try
        {
            using var document = PdfDocument.Open(pdfStream);
            var results = new List<PdfEmbeddedImage>();

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var getImagesMethod = page.GetType().GetMethod("GetImages", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (getImagesMethod == null) continue;

                if (getImagesMethod.Invoke(page, null) is not System.Collections.IEnumerable images)
                {
                    continue;
                }

                var pageIndex = page.Number;
                var imageIndex = 0;

                foreach (var image in images)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    imageIndex++;

                    var (bytes, contentType) = TryGetImageBytes(image);
                    var width = TryGetIntProperty(image, "Width") ?? TryGetIntProperty(image, "WidthInPoints");
                    var height = TryGetIntProperty(image, "Height") ?? TryGetIntProperty(image, "HeightInPoints");

                    results.Add(new PdfEmbeddedImage(bytes, contentType, pageIndex, imageIndex, width, height));
                }
            }

            return Task.FromResult(results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to extract embedded images from PDF.");
            return Task.FromResult(new List<PdfEmbeddedImage>());
        }
    }

    private static (byte[]? Bytes, string ContentType) TryGetImageBytes(object image)
    {
        var type = image.GetType();

        var tryGetPng = type.GetMethod("TryGetPngBytes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (tryGetPng != null)
        {
            var args = new object?[] { null };
            var ok = tryGetPng.Invoke(image, args) as bool?;
            if (ok == true && args[0] is byte[] pngBytes && pngBytes.Length > 0)
            {
                return (pngBytes, "image/png");
            }
        }

        var rawBytesProp = type.GetProperty("RawBytes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (rawBytesProp?.GetValue(image) is byte[] raw && raw.Length > 0)
        {
            return (raw, "application/octet-stream");
        }

        var bytesProp = type.GetProperty("Bytes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (bytesProp?.GetValue(image) is byte[] bytes && bytes.Length > 0)
        {
            return (bytes, "application/octet-stream");
        }

        return (null, "application/octet-stream");
    }

    private static int? TryGetIntProperty(object image, string propertyName)
    {
        var prop = image.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop == null) return null;

        var value = prop.GetValue(image);
        return value switch
        {
            int i => i,
            double d => (int)Math.Round(d),
            float f => (int)Math.Round(f),
            _ => null
        };
    }
}