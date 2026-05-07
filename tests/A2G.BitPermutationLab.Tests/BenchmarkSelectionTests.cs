using A2G.BitPermutationLab.Benchmarking;

namespace A2G.BitPermutationLab.Tests;

public sealed class BenchmarkSelectionTests
{
    [Fact]
    public void QuickProfile_IncludesAllRequiredBaselines()
    {
        IReadOnlyList<BenchmarkScenario> scenarios = BenchmarkProfileFactory.Create(BenchmarkProfileKind.Quick);

        string[] requiredScenarioIds =
        [
            "baseline-byte-identity-64:small",
            "baseline-hex-identity-32:small",
            "baseline-base64-identity-48:small",
            "xor-rotate-hex32:small",
            "feistel-base64-48:small"
        ];

        foreach (string scenarioId in requiredScenarioIds)
        {
            Assert.Contains(scenarios, scenario => scenario.ScenarioId == scenarioId && scenario.Weights.IsRequiredBaseline);
        }
    }

    [Fact]
    public void WeightedSelection_IsDeterministicForTheSameSeed()
    {
        BenchmarkSelectionOptions options = new(WeightingProfileKind.Balanced, 8, 1234);

        string[] first = BenchmarkProfileFactory.Create(options).Select(static scenario => scenario.ScenarioId).ToArray();
        string[] second = BenchmarkProfileFactory.Create(options).Select(static scenario => scenario.ScenarioId).ToArray();

        Assert.Equal(first, second);
    }

    [Fact]
    public void FullProfile_ReturnsEveryCandidateScenario()
    {
        IReadOnlyList<BenchmarkScenario> candidates = BenchmarkProfileFactory.CreateCandidates();
        IReadOnlyList<BenchmarkScenario> selected = BenchmarkProfileFactory.Create(BenchmarkProfileKind.Full);

        Assert.Equal(candidates.Count, selected.Count);
    }
}
