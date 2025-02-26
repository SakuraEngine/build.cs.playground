using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SB.Core
{
    public static class Json
    {
        private static JsonSerializerOptions serOpt = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static string Serialize<TValue>(TValue value) => JsonSerializer.Serialize(value, serOpt);
        public static TValue? Deserialize<TValue>(string json) => JsonSerializer.Deserialize<TValue?>(json);
    }
}
