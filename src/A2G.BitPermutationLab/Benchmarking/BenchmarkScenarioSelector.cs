namespace A2G.BitPermutationLab.Benchmarking;

public static class BenchmarkScenarioSelector
{
    public static IReadOnlyList<BenchmarkScenario> Select(
        IReadOnlyList<BenchmarkScenario> candidates,
        BenchmarkSelectionOptions options)
    {
        List<BenchmarkScenario> orderedCandidates = [.. candidates.OrderBy(static scenario => scenario.ScenarioId, StringComparer.Ordinal)];
        List<BenchmarkScenario> selected = [];

        if (options.IncludeRequiredBaselines)
        {
            selected.AddRange(orderedCandidates.Where(static scenario => scenario.Weights.IsRequiredBaseline));
        }

        int targetCount = ResolveTargetCount(options, orderedCandidates.Count, selected.Count);
        if (selected.Count >= targetCount)
        {
            return OrderForExecution(selected);
        }

        List<BenchmarkScenario> remainder = [.. orderedCandidates.Where(scenario => !selected.Contains(scenario))];
        Random random = new(CreateRandomSeed(options.SamplingSeed));

        while (selected.Count < targetCount && remainder.Count > 0)
        {
            BenchmarkScenario next = TakeWeightedScenario(remainder, random);
            selected.Add(next);
        }

        return OrderForExecution(selected);
    }

    private static int CreateRandomSeed(ulong samplingSeed)
    {
        return unchecked((int)(samplingSeed ^ (samplingSeed >> 32)));
    }

    private static int ResolveTargetCount(BenchmarkSelectionOptions options, int candidateCount, int requiredBaselineCount)
    {
        if (options.WeightingProfile == WeightingProfileKind.Exhaustive && options.ScenarioBudget is null)
        {
            return candidateCount;
        }

        if (options.ScenarioBudget is null)
        {
            return Math.Max(requiredBaselineCount, candidateCount);
        }

        return Math.Max(requiredBaselineCount, Math.Min(candidateCount, options.ScenarioBudget.Value));
    }

    private static BenchmarkScenario TakeWeightedScenario(List<BenchmarkScenario> candidates, Random random)
    {
        double totalWeight = candidates.Sum(candidate => GetEffectiveSelectionWeight(candidate));
        double threshold = random.NextDouble() * totalWeight;
        double running = 0d;

        for (int i = 0; i < candidates.Count; i++)
        {
            BenchmarkScenario candidate = candidates[i];
            running += GetEffectiveSelectionWeight(candidate);
            if (threshold <= running || i == candidates.Count - 1)
            {
                candidates.RemoveAt(i);
                return candidate;
            }
        }

        throw new InvalidOperationException("Weighted selection failed to choose a benchmark scenario.");
    }

    private static IReadOnlyList<BenchmarkScenario> OrderForExecution(List<BenchmarkScenario> scenarios)
    {
        return [.. scenarios
            .OrderByDescending(static scenario => scenario.Weights.IsRequiredBaseline)
            .ThenByDescending(static scenario => scenario.Weights.SelectionWeight)
            .ThenBy(static scenario => scenario.ScenarioId, StringComparer.Ordinal)];
    }

    private static double GetEffectiveSelectionWeight(BenchmarkScenario scenario)
    {
        double effectiveWeight = Math.Max(0.000001d, scenario.Weights.SelectionWeight);

        effectiveWeight *= scenario.Parameters.Emitter.EmitterKind switch
        {
            EmitterKind.ByteArray => 1.25,
            EmitterKind.Base64Url => 1.15,
            EmitterKind.Hex16 => 1.00,
            EmitterKind.Base32Crockford => 0.90,
            _ => 1.00
        };

        effectiveWeight *= scenario.Weights.IsRequiredBaseline ? 1.50 : 1.00;
        return effectiveWeight;
    }
}
