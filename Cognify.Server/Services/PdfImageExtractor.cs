using System.IO.Compression;
using System.Reflection;
using Docnet.Core;
using Docnet.Core.Models;
using Cognify.Server.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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
            var pageDimensions = new List<(int PageNumber, int Width, int Height)>();

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
                var widthPx = ToPixels(page.Width);
                var heightPx = ToPixels(page.Height);
                if (widthPx <= 0 || heightPx <= 0)
                {
                    (widthPx, heightPx) = (1240, 1754);
                }
                pageDimensions.Add((pageIndex, widthPx, heightPx));
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

            var hasRenderableImages = results.Any(r =>
                r.Bytes is { Length: > 0 } &&
                !string.IsNullOrWhiteSpace(r.ContentType) &&
                !r.ContentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase));

            if (!hasRenderableImages)
            {
                var rendered = TryRenderPagesAsImages(pdfStream, pageDimensions, cancellationToken);
                if (rendered.Count > 0)
                {
                    return Task.FromResult(rendered);
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

    private static int ToPixels(double points, int dpi = 150)
    {
        if (points <= 0) return 0;
        var pixels = (int)Math.Round(points * dpi / 72.0);
        return pixels <= 0 ? 0 : pixels;
    }

    private List<PdfEmbeddedImage> TryRenderPagesAsImages(
        Stream pdfStream,
        List<(int PageNumber, int Width, int Height)> pageDimensions,
        CancellationToken cancellationToken)
    {
        if (pageDimensions.Count == 0)
        {
            return new List<PdfEmbeddedImage>();
        }

        if (!pdfStream.CanSeek)
        {
            logger.LogWarning("PDF stream is not seekable. Skipping vector page rendering.");
            return new List<PdfEmbeddedImage>();
        }

        pdfStream.Position = 0;
        using var buffer = new MemoryStream();
        pdfStream.CopyTo(buffer);
        var pdfBytes = buffer.ToArray();

        var results = new List<PdfEmbeddedImage>();
        var imageIndex = 0;

        foreach (var page in pageDimensions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            imageIndex++;

            using var docReader = DocLib.Instance.GetDocReader(pdfBytes, new PageDimensions(page.Width, page.Height));
            using var pageReader = docReader.GetPageReader(page.PageNumber - 1);

            var raw = pageReader.GetImage();
            if (raw.Length == 0)
            {
                continue;
            }

            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();

            using var image = Image.LoadPixelData<Rgba32>(raw, width, height);
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            var pngBytes = ms.ToArray();

            results.Add(new PdfEmbeddedImage(
                pngBytes,
                "image/png",
                page.PageNumber,
                imageIndex,
                width,
                height));
        }

        return results;
    }

    private static (byte[]? Bytes, string ContentType) TryGetImageBytes(object image)
    {
        var type = image.GetType();

        var pngBytes = TryInvokeByteGetter(image, "TryGetPngBytes")
            ?? TryInvokeByteGetter(image, "GetPngBytes");
        if (pngBytes is { Length: > 0 })
        {
            return (pngBytes, "image/png");
        }

        var jpegBytes = TryInvokeByteGetter(image, "TryGetJpegBytes")
            ?? TryInvokeByteGetter(image, "GetJpegBytes")
            ?? TryInvokeByteGetter(image, "TryGetJpgBytes")
            ?? TryInvokeByteGetter(image, "GetJpgBytes");
        if (jpegBytes is { Length: > 0 })
        {
            return (jpegBytes, "image/jpeg");
        }

        var bytesProp = type.GetProperty("Bytes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (bytesProp?.GetValue(image) is byte[] bytes && bytes.Length > 0)
        {
            if (TryDetectContentType(bytes, out var detectedType))
            {
                return (bytes, detectedType);
            }

            if (TryBuildPngFromRaw(image, bytes, out var pngFromBytes))
            {
                return (pngFromBytes, "image/png");
            }
        }

        var rawBytesProp = type.GetProperty("RawBytes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (rawBytesProp?.GetValue(image) is byte[] raw && raw.Length > 0)
        {
            if (TryDetectContentType(raw, out var detectedType))
            {
                return (raw, detectedType);
            }

            if (TryBuildPngFromRaw(image, raw, out var pngFromRaw))
            {
                return (pngFromRaw, "image/png");
            }

            return (raw, "application/octet-stream");
        }

        return (null, "application/octet-stream");
    }

    private static byte[]? TryInvokeByteGetter(object image, string methodName)
    {
        var method = image.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null)
        {
            return null;
        }

        var parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            var result = method.Invoke(image, null);
            return TryExtractBytes(result);
        }

        if (parameters.Length == 1 && parameters[0].ParameterType.IsByRef)
        {
            var elementType = parameters[0].ParameterType.GetElementType();
            var args = new object?[] { elementType == typeof(byte[]) ? Array.Empty<byte>() : Activator.CreateInstance(elementType!) };
            var result = method.Invoke(image, args);

            if (result is bool ok && !ok)
            {
                return null;
            }

            return TryExtractBytes(args[0]);
        }

        return null;
    }

    private static byte[]? TryExtractBytes(object? value)
    {
        return value switch
        {
            byte[] bytes => bytes.Length > 0 ? bytes : null,
            ReadOnlyMemory<byte> rom => rom.Length > 0 ? rom.ToArray() : null,
            Memory<byte> mem => mem.Length > 0 ? mem.ToArray() : null,
            _ => null
        };
    }

    private static bool TryDetectContentType(byte[] bytes, out string contentType)
    {
        contentType = "application/octet-stream";

        if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
        {
            contentType = "image/png";
            return true;
        }

        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            contentType = "image/jpeg";
            return true;
        }

        if (bytes.Length >= 12 && bytes[4] == 0x6A && bytes[5] == 0x50 && bytes[6] == 0x20 && bytes[7] == 0x20)
        {
            contentType = "image/jp2";
            return true;
        }

        return false;
    }

    private static bool TryBuildPngFromRaw(object image, byte[] inputBytes, out byte[] pngBytes)
    {
        pngBytes = Array.Empty<byte>();

        var width = TryGetIntProperty(image, "Width") ?? TryGetIntProperty(image, "WidthInPoints");
        var height = TryGetIntProperty(image, "Height") ?? TryGetIntProperty(image, "HeightInPoints");
        if (width is null || height is null || width <= 0 || height <= 0)
        {
            return false;
        }

        var bitsPerComponent = TryGetIntProperty(image, "BitsPerComponent") ?? 8;
        var colorSpace = TryGetColorSpaceName(image) ?? "";

        var rawBytes = inputBytes;
        if (LooksLikeZlib(rawBytes) && TryDecompressZlib(rawBytes, out var decompressed))
        {
            rawBytes = decompressed;
        }

        if (TryCreatePngFromPixelData(rawBytes, width.Value, height.Value, bitsPerComponent, colorSpace, out pngBytes))
        {
            return true;
        }

        return false;
    }

    private static bool LooksLikeZlib(byte[] bytes)
    {
        return bytes.Length >= 2 && bytes[0] == 0x78 && (bytes[1] == 0x01 || bytes[1] == 0x5E || bytes[1] == 0x9C || bytes[1] == 0xDA);
    }

    private static bool TryDecompressZlib(byte[] bytes, out byte[] decompressed)
    {
        try
        {
            using var input = new MemoryStream(bytes);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);
            decompressed = output.ToArray();
            return decompressed.Length > 0;
        }
        catch
        {
            decompressed = Array.Empty<byte>();
            return false;
        }
    }

    private static string? TryGetColorSpaceName(object image)
    {
        var colorSpaceProp = image.GetType().GetProperty("ColorSpace", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? image.GetType().GetProperty("ColorSpaceDetails", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var value = colorSpaceProp?.GetValue(image);
        if (value == null)
        {
            return null;
        }

        if (value is string str)
        {
            return str;
        }

        var nameProp = value.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? value.GetType().GetProperty("Type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        return nameProp?.GetValue(value)?.ToString();
    }

    private static string NormalizeColorSpaceName(string? colorSpace)
    {
        if (string.IsNullOrWhiteSpace(colorSpace))
        {
            return string.Empty;
        }

        return colorSpace
            .Replace("/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToLowerInvariant();
    }

    private static bool IsGrayColorSpace(string colorSpaceName)
    {
        if (string.IsNullOrWhiteSpace(colorSpaceName)) return false;
        return colorSpaceName.Contains("gray", StringComparison.OrdinalIgnoreCase)
            || colorSpaceName.Contains("devicegray", StringComparison.OrdinalIgnoreCase)
            || colorSpaceName.Equals("g", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRgbColorSpace(string colorSpaceName)
    {
        if (string.IsNullOrWhiteSpace(colorSpaceName)) return false;
        return colorSpaceName.Contains("rgb", StringComparison.OrdinalIgnoreCase)
            || colorSpaceName.Contains("devicergb", StringComparison.OrdinalIgnoreCase)
            || colorSpaceName.Contains("iccbased", StringComparison.OrdinalIgnoreCase)
            || colorSpaceName.Equals("r", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCmykColorSpace(string colorSpaceName)
    {
        if (string.IsNullOrWhiteSpace(colorSpaceName)) return false;
        return colorSpaceName.Contains("cmyk", StringComparison.OrdinalIgnoreCase)
            || colorSpaceName.Contains("devicecmyk", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryCreatePngFromPixelData(byte[] rawBytes, int width, int height, int bitsPerComponent, string colorSpace, out byte[] pngBytes)
    {
        pngBytes = Array.Empty<byte>();
        var colorSpaceName = NormalizeColorSpaceName(colorSpace);

        if (bitsPerComponent <= 0)
        {
            bitsPerComponent = 8;
        }

        var isGray = IsGrayColorSpace(colorSpaceName);
        var isRgb = IsRgbColorSpace(colorSpaceName);
        var isCmyk = IsCmykColorSpace(colorSpaceName);

        if (bitsPerComponent == 1 && isGray)
        {
            var stride = (width + 7) / 8;
            if (rawBytes.Length < stride * height)
            {
                return false;
            }

            var expanded = new byte[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var byteIndex = y * stride + (x / 8);
                    var bitIndex = 7 - (x % 8);
                    var bit = (rawBytes[byteIndex] >> bitIndex) & 0x1;
                    expanded[y * width + x] = bit == 1 ? (byte)255 : (byte)0;
                }
            }

            using var img = Image.LoadPixelData<L8>(expanded, width, height);
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            pngBytes = ms.ToArray();
            return pngBytes.Length > 0;
        }

        if (bitsPerComponent == 8 && isGray)
        {
            if (rawBytes.Length < width * height)
            {
                return false;
            }

            using var img = Image.LoadPixelData<L8>(rawBytes, width, height);
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            pngBytes = ms.ToArray();
            return pngBytes.Length > 0;
        }

        if (bitsPerComponent == 8 && isRgb)
        {
            if (rawBytes.Length < width * height * 3)
            {
                return false;
            }

            using var img = Image.LoadPixelData<Rgb24>(rawBytes, width, height);
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            pngBytes = ms.ToArray();
            return pngBytes.Length > 0;
        }

        if (bitsPerComponent == 8 && isCmyk)
        {
            if (rawBytes.Length < width * height * 4)
            {
                return false;
            }

            var rgb = new byte[width * height * 3];
            for (var i = 0; i < width * height; i++)
            {
                var c = rawBytes[i * 4 + 0];
                var m = rawBytes[i * 4 + 1];
                var y = rawBytes[i * 4 + 2];
                var k = rawBytes[i * 4 + 3];

                var r = (byte)(255 - Math.Min(255, c + k));
                var g = (byte)(255 - Math.Min(255, m + k));
                var b = (byte)(255 - Math.Min(255, y + k));

                var offset = i * 3;
                rgb[offset] = r;
                rgb[offset + 1] = g;
                rgb[offset + 2] = b;
            }

            using var img = Image.LoadPixelData<Rgb24>(rgb, width, height);
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            pngBytes = ms.ToArray();
            return pngBytes.Length > 0;
        }

        // Fallback: infer format by byte length if metadata is missing or unknown.

        var pixelCount = (long)width * height;
        if (pixelCount <= 0)
        {
            return false;
        }

        var stride1Bit = (width + 7L) / 8L;
        if (rawBytes.Length == stride1Bit * height)
        {
            var expanded = new byte[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var byteIndex = y * stride1Bit + (x / 8);
                    var bitIndex = 7 - (x % 8);
                    var bit = (rawBytes[byteIndex] >> bitIndex) & 0x1;
                    expanded[y * width + x] = bit == 1 ? (byte)255 : (byte)0;
                }
            }

            using var img = Image.LoadPixelData<L8>(expanded, width, height);
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            pngBytes = ms.ToArray();
            return pngBytes.Length > 0;
        }

        if (rawBytes.Length == pixelCount)
        {
            using var img = Image.LoadPixelData<L8>(rawBytes, width, height);
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            pngBytes = ms.ToArray();
            return pngBytes.Length > 0;
        }

        if (rawBytes.Length == pixelCount * 3)
        {
            using var img = Image.LoadPixelData<Rgb24>(rawBytes, width, height);
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            pngBytes = ms.ToArray();
            return pngBytes.Length > 0;
        }

        if (rawBytes.Length == pixelCount * 4)
        {
            var rgb = new byte[width * height * 3];
            for (var i = 0; i < width * height; i++)
            {
                var c = rawBytes[i * 4 + 0];
                var m = rawBytes[i * 4 + 1];
                var y = rawBytes[i * 4 + 2];
                var k = rawBytes[i * 4 + 3];

                var r = (byte)(255 - Math.Min(255, c + k));
                var g = (byte)(255 - Math.Min(255, m + k));
                var b = (byte)(255 - Math.Min(255, y + k));

                var offset = i * 3;
                rgb[offset] = r;
                rgb[offset + 1] = g;
                rgb[offset + 2] = b;
            }

            using var img = Image.LoadPixelData<Rgb24>(rgb, width, height);
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            pngBytes = ms.ToArray();
            return pngBytes.Length > 0;
        }

        return false;
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