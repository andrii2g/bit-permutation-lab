namespace A2G.BitPermutationLab.Benchmarking;

public sealed record WeightingOverrides(
    WeightingProfileKind? Profile,
    int? ScenarioBudget,
    ulong? SamplingSeed,
    bool? IncludeRequiredBaselines,
    bool? IncludeRawUnweightedReport,
    IReadOnlyDictionary<string, WeightOverrideValues> MixerOverrides,
    IReadOnlyDictionary<string, WeightOverrideValues> PermutationOverrides,
    IReadOnlyDictionary<string, WeightOverrideValues> EmitterOverrides)
{
    public static WeightingOverrides Empty { get; } = new(
        null,
        null,
        null,
        null,
        null,
        new Dictionary<string, WeightOverrideValues>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, WeightOverrideValues>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, WeightOverrideValues>(StringComparer.OrdinalIgnoreCase));

    public WeightingOverrides Merge(WeightingOverrides other)
    {
        return new WeightingOverrides(
            other.Profile ?? Profile,
            other.ScenarioBudget ?? ScenarioBudget,
            other.SamplingSeed ?? SamplingSeed,
            other.IncludeRequiredBaselines ?? IncludeRequiredBaselines,
            other.IncludeRawUnweightedReport ?? IncludeRawUnweightedReport,
            MergeDictionaries(MixerOverrides, other.MixerOverrides),
            MergeDictionaries(PermutationOverrides, other.PermutationOverrides),
            MergeDictionaries(EmitterOverrides, other.EmitterOverrides));
    }

    private static IReadOnlyDictionary<string, WeightOverrideValues> MergeDictionaries(
        IReadOnlyDictionary<string, WeightOverrideValues> primary,
        IReadOnlyDictionary<string, WeightOverrideValues> secondary)
    {
        Dictionary<string, WeightOverrideValues> merged = new(primary, StringComparer.OrdinalIgnoreCase);
        foreach ((string key, WeightOverrideValues value) in secondary)
        {
            merged[key] = merged.TryGetValue(key, out WeightOverrideValues? existing)
                ? existing.Merge(value)
                : value;
        }

        return merged;
    }
}

public sealed record WeightOverrideValues(
    double? AlgorithmWeight,
    double? ExpectedCostFactor)
{
    public WeightOverrideValues Merge(WeightOverrideValues other)
    {
        return new WeightOverrideValues(
            other.AlgorithmWeight ?? AlgorithmWeight,
            other.ExpectedCostFactor ?? ExpectedCostFactor);
    }
}
