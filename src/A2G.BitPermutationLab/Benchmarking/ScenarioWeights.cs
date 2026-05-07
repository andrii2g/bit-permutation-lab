namespace A2G.BitPermutationLab.Benchmarking;

public sealed record ScenarioWeights(
    double AlgorithmWeight,
    double ParameterTierWeight,
    double ValueRangeWeight,
    double EmitterWeight,
    double CustomMutationWeight,
    double ExpectedCostFactor,
    double SelectionWeight,
    bool IsRequiredBaseline);
