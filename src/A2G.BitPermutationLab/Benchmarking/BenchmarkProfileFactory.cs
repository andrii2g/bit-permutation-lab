using A2G.BitPermutationLab.Binary;
using A2G.BitPermutationLab.Chunking;
using A2G.BitPermutationLab.Core;
using A2G.BitPermutationLab.Emitters;
using A2G.BitPermutationLab.Mixers;
using A2G.BitPermutationLab.Permutations;

namespace A2G.BitPermutationLab.Benchmarking;

public static class BenchmarkProfileFactory
{
    public static IReadOnlyList<BenchmarkScenario> Create(BenchmarkProfileKind profileKind)
    {
        return Create(CreateSelectionOptions(profileKind));
    }

    public static IReadOnlyList<BenchmarkScenario> Create(BenchmarkSelectionOptions options)
    {
        return BenchmarkScenarioSelector.Select(CreateCandidates(), options);
    }

    public static IReadOnlyList<BenchmarkScenario> CreateCandidates()
    {
        List<BenchmarkScenario> scenarios = [];

        scenarios.AddRange(CreateValueRangeVariants(
            "baseline-byte-identity-64",
            "baseline-byte-identity-64",
            CreateParameters(
                "baseline-byte-identity-64",
                NumberKind.UInt64,
                64,
                0UL,
                new MixerParameters(MixerKind.None),
                new PermutationParameters(PermutationKind.Identity),
                new ChunkingParameters(ChunkerKind.Fixed, 8),
                new EmitterParameters(EmitterKind.ByteArray, AlphabetKind.None, OutputKind.ByteArray)),
            ParameterTierKind.Min,
            algorithmWeight: 0.15,
            emitterWeight: 1.50,
            expectedCostFactor: 0.25,
            requiredBaselineRange: ValueRangeKind.Small));

        scenarios.AddRange(CreateValueRangeVariants(
            "baseline-hex-identity-32",
            "baseline-hex-identity-32",
            CreateParameters(
                "baseline-hex-identity-32",
                NumberKind.UInt32,
                32,
                0UL,
                new MixerParameters(MixerKind.None),
                new PermutationParameters(PermutationKind.Identity),
                new ChunkingParameters(ChunkerKind.Fixed, 4),
                new EmitterParameters(EmitterKind.Hex16, AlphabetKind.Hex16, OutputKind.String)),
            ParameterTierKind.Min,
            algorithmWeight: 0.20,
            emitterWeight: 1.20,
            expectedCostFactor: 0.40,
            requiredBaselineRange: ValueRangeKind.Small));

        scenarios.AddRange(CreateValueRangeVariants(
            "baseline-base64-identity-48",
            "baseline-base64-identity-48",
            CreateParameters(
                "baseline-base64-identity-48",
                NumberKind.UInt64,
                48,
                0UL,
                new MixerParameters(MixerKind.None),
                new PermutationParameters(PermutationKind.Identity),
                new ChunkingParameters(ChunkerKind.Fixed, 6),
                new EmitterParameters(EmitterKind.Base64Url, AlphabetKind.Base64Url, OutputKind.String)),
            ParameterTierKind.Middle,
            algorithmWeight: 0.30,
            emitterWeight: 1.30,
            expectedCostFactor: 0.50,
            requiredBaselineRange: ValueRangeKind.Small));

        scenarios.AddRange(CreateValueRangeVariants(
            "xor-rotate-hex32",
            "xor-rotate-hex32",
            CreateParameters(
                "xor-rotate-hex32",
                NumberKind.UInt32,
                32,
                42UL,
                new MixerParameters(MixerKind.Xor),
                new PermutationParameters(PermutationKind.Rotate, RotateBy: 11),
                new ChunkingParameters(ChunkerKind.Fixed, 4),
                new EmitterParameters(EmitterKind.Hex16, AlphabetKind.Hex16, OutputKind.String)),
            ParameterTierKind.Middle,
            algorithmWeight: 1.25,
            emitterWeight: 1.20,
            expectedCostFactor: 0.70,
            requiredBaselineRange: ValueRangeKind.Small));

        scenarios.AddRange(CreateValueRangeVariants(
            "add-bitreverse-base64-48",
            "add-bitreverse-base64-48",
            CreateParameters(
                "add-bitreverse-base64-48",
                NumberKind.UInt64,
                48,
                123UL,
                new MixerParameters(MixerKind.Add),
                new PermutationParameters(PermutationKind.BitReverse),
                new ChunkingParameters(ChunkerKind.Fixed, 6),
                new EmitterParameters(EmitterKind.Base64Url, AlphabetKind.Base64Url, OutputKind.String)),
            ParameterTierKind.Middle,
            algorithmWeight: 0.95,
            emitterWeight: 1.30,
            expectedCostFactor: 1.10));

        scenarios.AddRange(CreateValueRangeVariants(
            "multiply-byteswap-base32-40",
            "multiply-byteswap-base32-40",
            CreateParameters(
                "multiply-byteswap-base32-40",
                NumberKind.UInt64,
                40,
                99UL,
                new MixerParameters(MixerKind.Multiply),
                new PermutationParameters(PermutationKind.ByteSwap),
                new ChunkingParameters(ChunkerKind.Fixed, 5),
                new EmitterParameters(EmitterKind.Base32Crockford, AlphabetKind.Base32Crockford, OutputKind.String)),
            ParameterTierKind.Middle,
            algorithmWeight: 0.85,
            emitterWeight: 0.80,
            expectedCostFactor: 1.00));

        scenarios.AddRange(CreateValueRangeVariants(
            "nibbleswap-hex32",
            "nibbleswap-hex32",
            CreateParameters(
                "nibbleswap-hex32",
                NumberKind.UInt32,
                32,
                0UL,
                new MixerParameters(MixerKind.None),
                new PermutationParameters(PermutationKind.NibbleSwap, NibbleSwap: NibbleSwapKind.ReverseNibbles),
                new ChunkingParameters(ChunkerKind.Fixed, 4),
                new EmitterParameters(EmitterKind.Hex16, AlphabetKind.Hex16, OutputKind.String)),
            ParameterTierKind.Middle,
            algorithmWeight: 0.70,
            emitterWeight: 1.20,
            expectedCostFactor: 1.20));

        scenarios.AddRange(CreateValueRangeVariants(
            "chunkperm-base32-40",
            "chunkperm-base32-40",
            CreateParameters(
                "chunkperm-base32-40",
                NumberKind.UInt64,
                40,
                321UL,
                new MixerParameters(MixerKind.Add),
                new PermutationParameters(
                    PermutationKind.ChunkPermutation,
                    ChunkPermutationGroupSize: 5,
                    ChunkPermutationVariant: ChunkPermutationVariant.ReverseGroups),
                new ChunkingParameters(ChunkerKind.Fixed, 5),
                new EmitterParameters(EmitterKind.Base32Crockford, AlphabetKind.Base32Crockford, OutputKind.String)),
            ParameterTierKind.Explicit,
            algorithmWeight: 1.30,
            emitterWeight: 0.80,
            expectedCostFactor: 1.50));

        scenarios.AddRange(CreateValueRangeVariants(
            "feistel-base64-48",
            "feistel-base64-48",
            CreateParameters(
                "feistel-base64-48",
                NumberKind.UInt64,
                48,
                777UL,
                new MixerParameters(MixerKind.Xor),
                new PermutationParameters(
                    PermutationKind.Feistel,
                    FeistelRounds: 2,
                    FeistelRoundFunction: FeistelRoundFunctionKind.XorShiftAdd),
                new ChunkingParameters(ChunkerKind.Fixed, 6),
                new EmitterParameters(EmitterKind.Base64Url, AlphabetKind.Base64Url, OutputKind.String)),
            ParameterTierKind.Explicit,
            algorithmWeight: 1.10,
            emitterWeight: 1.30,
            expectedCostFactor: 3.00,
            requiredBaselineRange: ValueRangeKind.Small));

        return scenarios;
    }

    public static BenchmarkSelectionOptions CreateSelectionOptions(BenchmarkProfileKind profileKind)
    {
        return profileKind switch
        {
            BenchmarkProfileKind.Quick => new BenchmarkSelectionOptions(WeightingProfileKind.SpeedFirst, 6, 11),
            BenchmarkProfileKind.Default => new BenchmarkSelectionOptions(WeightingProfileKind.Balanced, 10, 29),
            BenchmarkProfileKind.Full => new BenchmarkSelectionOptions(WeightingProfileKind.Exhaustive, null, 47),
            _ => throw new ArgumentOutOfRangeException(nameof(profileKind), profileKind, "Unsupported benchmark profile.")
        };
    }

    private static IReadOnlyList<BenchmarkScenario> CreateValueRangeVariants(
        string scenarioIdStem,
        string nameStem,
        CodecParameters parameters,
        ParameterTierKind parameterTier,
        double algorithmWeight,
        double emitterWeight,
        double expectedCostFactor,
        ValueRangeKind? requiredBaselineRange = null)
    {
        return
        [
            CreateScenarioVariant(scenarioIdStem, nameStem, parameters, parameterTier, ValueRangeKind.Tiny, algorithmWeight, emitterWeight, expectedCostFactor, requiredBaselineRange == ValueRangeKind.Tiny),
            CreateScenarioVariant(scenarioIdStem, nameStem, parameters, parameterTier, ValueRangeKind.Small, algorithmWeight, emitterWeight, expectedCostFactor, requiredBaselineRange == ValueRangeKind.Small),
            CreateScenarioVariant(scenarioIdStem, nameStem, parameters, parameterTier, ValueRangeKind.Large, algorithmWeight, emitterWeight, expectedCostFactor, requiredBaselineRange == ValueRangeKind.Large)
        ];
    }

    private static BenchmarkScenario CreateScenarioVariant(
        string scenarioIdStem,
        string nameStem,
        CodecParameters parameters,
        ParameterTierKind parameterTier,
        ValueRangeKind valueRangeKind,
        double algorithmWeight,
        double emitterWeight,
        double expectedCostFactor,
        bool isRequiredBaseline)
    {
        IReadOnlyList<ulong> values = GetValues(valueRangeKind);
        ScenarioWeights weights = CreateWeights(parameters, parameterTier, valueRangeKind, algorithmWeight, emitterWeight, expectedCostFactor, isRequiredBaseline);
        string suffix = valueRangeKind.ToString().ToLowerInvariant();

        return new BenchmarkScenario(
            $"{scenarioIdStem}:{suffix}",
            $"{nameStem}-{suffix}",
            parameters with { Name = $"{parameters.Name}-{suffix}" },
            valueRangeKind,
            values,
            weights);
    }

    private static ScenarioWeights CreateWeights(
        CodecParameters parameters,
        ParameterTierKind parameterTier,
        ValueRangeKind valueRangeKind,
        double algorithmWeight,
        double emitterWeight,
        double expectedCostFactor,
        bool isRequiredBaseline)
    {
        double parameterTierWeight = GetParameterTierWeight(parameterTier);
        double valueRangeWeight = GetValueRangeWeight(valueRangeKind);
        double customMutationWeight = parameters.CustomMutation is null && parameters.CustomChunkMutation is null ? 1.00 : 1.20;
        double selectionWeight = algorithmWeight
            * parameterTierWeight
            * valueRangeWeight
            * emitterWeight
            * customMutationWeight
            / expectedCostFactor;

        return new ScenarioWeights(
            algorithmWeight,
            parameterTierWeight,
            valueRangeWeight,
            emitterWeight,
            customMutationWeight,
            expectedCostFactor,
            selectionWeight,
            isRequiredBaseline);
    }

    public static IReadOnlyList<ulong> GetValues(ValueRangeKind kind)
    {
        return kind switch
        {
            ValueRangeKind.Tiny => [1UL, 2UL],
            ValueRangeKind.Small => [1_000UL, 1_001UL, 4_095UL, 4_096UL, 8_191UL, 8_192UL, 9_999UL, 10_000UL],
            ValueRangeKind.Large => [1_000_000UL, 1_000_001UL, 16_777_215UL, 16_777_216UL, 268_435_455UL, 268_435_456UL, 999_999_999UL, 1_000_000_000UL],
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported value range.")
        };
    }

    private static double GetParameterTierWeight(ParameterTierKind kind)
    {
        return kind switch
        {
            ParameterTierKind.Min => 0.70,
            ParameterTierKind.Middle => 1.00,
            ParameterTierKind.Max => 0.60,
            ParameterTierKind.SaltDerived => 0.90,
            ParameterTierKind.Explicit => 1.00,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported parameter tier.")
        };
    }

    private static double GetValueRangeWeight(ValueRangeKind kind)
    {
        return kind switch
        {
            ValueRangeKind.Tiny => 0.50,
            ValueRangeKind.Small => 1.00,
            ValueRangeKind.Large => 1.00,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported value range.")
        };
    }

    private static CodecParameters CreateParameters(
        string name,
        NumberKind numberKind,
        int bitLength,
        ulong saltSeed,
        MixerParameters mixer,
        PermutationParameters permutation,
        ChunkingParameters chunking,
        EmitterParameters emitter)
    {
        return new CodecParameters(
            name,
            numberKind,
            bitLength,
            saltSeed,
            new BinaryParameters(BinaryKind.FixedUnsigned),
            mixer,
            permutation,
            chunking,
            emitter);
    }
}
