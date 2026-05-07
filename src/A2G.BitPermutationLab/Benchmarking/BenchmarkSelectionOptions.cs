namespace A2G.BitPermutationLab.Benchmarking;

public sealed record BenchmarkSelectionOptions(
    WeightingProfileKind WeightingProfile,
    int? ScenarioBudget,
    ulong SamplingSeed,
    bool IncludeRequiredBaselines = true);
