using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SB.Core
{
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ImmutableSortedDictionary<string, DateTime>))]
    [JsonSerializable(typeof(Depend))]
    [JsonSerializable(typeof(CLDependenciesData))]
    [JsonSerializable(typeof(CLDependencies))]
    internal partial class JsonContext : JsonSerializerContext
    {

    }
}
