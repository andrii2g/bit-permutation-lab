namespace A2G.BitPermutationLab.Benchmarking;

public sealed record BenchmarkSelectionOptions(
    WeightingProfileKind WeightingProfile,
    int? ScenarioBudget,
    int SamplingSeed,
    bool IncludeRequiredBaselines = true);
