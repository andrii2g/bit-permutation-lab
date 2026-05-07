using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2G.BitPermutationLab.Benchmarking;

public static class BenchmarkWeightingConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static WeightingOverrides Load(string path)
    {
        WeightingRootDocument document = JsonSerializer.Deserialize<WeightingRootDocument>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException("Weighting config could not be parsed.");

        WeightingDocument? weighting = document.Weighting ?? document.DirectWeighting;
        if (weighting is null)
        {
            return WeightingOverrides.Empty;
        }

        return ToOverrides(weighting);
    }

    internal static WeightingOverrides ToOverrides(WeightingDocument document)
    {
        return new WeightingOverrides(
            ParseNullableProfile(document.Profile),
            document.ScenarioBudget,
            document.SamplingSeed,
            document.IncludeRequiredBaselines,
            document.IncludeRawUnweightedReport,
            ToDictionary(document.Weights?.Mixers),
            ToDictionary(document.Weights?.Permutations),
            ToDictionary(document.Weights?.Emitters));
    }

    private static IReadOnlyDictionary<string, WeightOverrideValues> ToDictionary(Dictionary<string, WeightOverrideDocument>? values)
    {
        if (values is null || values.Count == 0)
        {
            return new Dictionary<string, WeightOverrideValues>(StringComparer.OrdinalIgnoreCase);
        }

        return values.ToDictionary(
            static pair => pair.Key,
            static pair => new WeightOverrideValues(pair.Value.AlgorithmWeight, pair.Value.ExpectedCostFactor),
            StringComparer.OrdinalIgnoreCase);
    }

    private static WeightingProfileKind? ParseNullableProfile(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Enum.TryParse<WeightingProfileKind>(value, true, out WeightingProfileKind parsed)
            ? parsed
            : throw new InvalidOperationException($"Unsupported weighting.profile '{value}'.");
    }
}

internal sealed record WeightingRootDocument(
    [property: JsonPropertyName("weighting")] WeightingDocument? Weighting)
{
    [JsonIgnore]
    public WeightingDocument? DirectWeighting => Weighting;
}

internal sealed record WeightingDocument(
    [property: JsonPropertyName("profile")] string? Profile,
    [property: JsonPropertyName("scenarioBudget")] int? ScenarioBudget,
    [property: JsonPropertyName("samplingSeed")] ulong? SamplingSeed,
    [property: JsonPropertyName("includeRequiredBaselines")] bool? IncludeRequiredBaselines,
    [property: JsonPropertyName("includeRawUnweightedReport")] bool? IncludeRawUnweightedReport,
    [property: JsonPropertyName("weights")] WeightBucketsDocument? Weights);

internal sealed record WeightBucketsDocument(
    [property: JsonPropertyName("mixers")] Dictionary<string, WeightOverrideDocument>? Mixers,
    [property: JsonPropertyName("permutations")] Dictionary<string, WeightOverrideDocument>? Permutations,
    [property: JsonPropertyName("emitters")] Dictionary<string, WeightOverrideDocument>? Emitters);

internal sealed record WeightOverrideDocument(
    [property: JsonPropertyName("algorithmWeight")] double? AlgorithmWeight,
    [property: JsonPropertyName("expectedCostFactor")] double? ExpectedCostFactor);
