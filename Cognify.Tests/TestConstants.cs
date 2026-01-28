using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cognify.Tests;

public static class TestConstants
{
    public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
