using System.Text.Json;
using System.Text.Json.Serialization;
using A2G.BitPermutationLab.Binary;
using A2G.BitPermutationLab.Chunking;
using A2G.BitPermutationLab.Core;
using A2G.BitPermutationLab.Custom;
using A2G.BitPermutationLab.Emitters;
using A2G.BitPermutationLab.Mixers;
using A2G.BitPermutationLab.Permutations;

namespace A2G.BitPermutationLab.Benchmarking;

public static class BenchmarkConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static LoadedBenchmarkConfig Load(string path)
    {
        string baseDirectory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? Directory.GetCurrentDirectory();
        BenchmarkConfigDocument document = JsonSerializer.Deserialize<BenchmarkConfigDocument>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException("Benchmark config could not be parsed.");

        List<BenchmarkScenario> scenarios = [];
        foreach (BenchmarkScenarioDocument scenario in document.Scenarios ?? [])
        {
            scenarios.Add(ToScenario(scenario, baseDirectory));
        }

        BenchmarkExecutionDocument benchmark = document.Benchmark ?? new BenchmarkExecutionDocument();
        BenchmarkModeKind mode = ParseEnum(benchmark.Mode, BenchmarkModeKind.Quick);
        int iterations = benchmark.Iterations ?? 1_000;
        if (iterations <= 0)
        {
            throw new InvalidOperationException("Config benchmark.iterations must be greater than zero.");
        }

        BenchmarkExecutionOptions options = new(
            "Config",
            mode.ToString(),
            iterations,
            new BenchmarkSelectionOptions(
                WeightingProfileKind.Balanced,
                null,
                0UL,
                true),
            BenchmarkReportOptions.Default,
            benchmark.Validate ?? true);

        return new LoadedBenchmarkConfig(
            scenarios,
            options,
            benchmark.OutputMarkdown,
            benchmark.OutputCsv,
            benchmark.IncludeInvalid ?? true,
            ParseEnum(benchmark.ValueSet, BenchmarkValueSetKind.Default),
            benchmark.Top ?? 5);
    }

    private static BenchmarkScenario ToScenario(BenchmarkScenarioDocument document, string baseDirectory)
    {
        if (document.Parameters is null)
        {
            throw new InvalidOperationException($"Scenario '{document.Name ?? "<unnamed>"}' is missing parameters.");
        }

        CodecParameters parameters = ToParameters(document.Name, document.Parameters, baseDirectory);
        IReadOnlyList<ulong> values = document.Values?.Count > 0
            ? document.Values
            : BenchmarkProfileFactory.GetValues(ValueRangeKind.Small);

        ValueRangeKind rangeKind = InferValueRange(values);
        ScenarioWeights weights = new(
            1.00,
            1.00,
            GetValueRangeWeight(rangeKind),
            1.00,
            parameters.CustomMutation is null && parameters.CustomChunkMutation is null ? 1.00 : 1.20,
            1.00,
            GetValueRangeWeight(rangeKind),
            false);

        string scenarioName = document.Name ?? parameters.Name;
        string scenarioId = $"{scenarioName}:{rangeKind.ToString().ToLowerInvariant()}";

        return new BenchmarkScenario(scenarioId, scenarioName, parameters with { Name = scenarioName }, rangeKind, values, weights);
    }

    private static CodecParameters ToParameters(string? scenarioName, CodecParametersDocument document, string baseDirectory)
    {
        ulong saltSeed = ResolveSaltSeed(document.SaltSeed, document.SaltText);
        return new CodecParameters(
            document.Name ?? scenarioName ?? "config-scenario",
            ParseRequiredEnum<NumberKind>(document.NumberKind, nameof(document.NumberKind)),
            document.BitLength ?? throw new InvalidOperationException("bitLength is required."),
            saltSeed,
            ToBinary(document.Binary),
            ToMixer(document.Mixer),
            ToPermutation(document.Permutation),
            ToChunking(document.Chunking),
            ToEmitter(document.Emitter),
            ToCustomMutation(document.CustomMutation, baseDirectory),
            ToCustomChunkMutation(document.CustomChunkMutation, baseDirectory));
    }

    private static BinaryParameters ToBinary(BinaryParametersDocument? document)
    {
        if (document is null)
        {
            return new BinaryParameters(BinaryKind.FixedUnsigned);
        }

        return new BinaryParameters(
            ParseRequiredEnum<BinaryKind>(document.Kind, nameof(document.Kind)),
            ParseEnum(document.BitOrder, BitOrderKind.MsbFirst),
            ParseEnum(document.ByteOrder, ByteOrderKind.BigEndian));
    }

    private static MixerParameters ToMixer(MixerParametersDocument? document)
    {
        if (document is null)
        {
            return new MixerParameters(MixerKind.None);
        }

        return new MixerParameters(
            ParseRequiredEnum<MixerKind>(document.Kind, nameof(document.Kind)),
            ParseEnum(document.MaskDerivation, SaltDerivationKind.SplitMix64),
            document.LiteralMask,
            document.LiteralAddend,
            document.Multiplier);
    }

    private static PermutationParameters ToPermutation(PermutationParametersDocument? document)
    {
        if (document is null)
        {
            return new PermutationParameters(PermutationKind.Identity);
        }

        return new PermutationParameters(
            ParseRequiredEnum<PermutationKind>(document.Kind, nameof(document.Kind)),
            document.RotateBy,
            ParseNullableEnum<NibbleSwapKind>(document.NibbleSwap),
            document.ChunkPermutationGroupSize,
            ParseNullableEnum<ChunkPermutationVariant>(document.ChunkPermutationVariant),
            document.ChunkPermutationOrder,
            document.ChunkPermutationRotateBy,
            document.FeistelRounds,
            ParseNullableEnum<FeistelRoundFunctionKind>(document.FeistelRoundFunction));
    }

    private static ChunkingParameters ToChunking(ChunkingParametersDocument? document)
    {
        if (document is null)
        {
            throw new InvalidOperationException("chunking is required.");
        }

        return new ChunkingParameters(
            ParseRequiredEnum<ChunkerKind>(document.Kind, nameof(document.Kind)),
            document.ChunkSize ?? throw new InvalidOperationException("chunkSize is required."),
            ParseEnum(document.ChunkReadOrder, BitOrderKind.MsbFirst));
    }

    private static EmitterParameters ToEmitter(EmitterParametersDocument? document)
    {
        if (document is null)
        {
            throw new InvalidOperationException("emitter is required.");
        }

        return new EmitterParameters(
            ParseRequiredEnum<EmitterKind>(document.Kind, nameof(document.Kind)),
            ParseRequiredEnum<AlphabetKind>(document.AlphabetKind, nameof(document.AlphabetKind)),
            ParseRequiredEnum<OutputKind>(document.OutputKind, nameof(document.OutputKind)),
            document.CustomAlphabet,
            ParseEnum(document.ByteArrayTextFormat, ByteArrayTextFormat.Hex));
    }

    private static CustomMutationParameters? ToCustomMutation(CustomMutationParametersDocument? document, string baseDirectory)
    {
        if (document is null)
        {
            return null;
        }

        string? name = document.Name;
        if (!string.IsNullOrWhiteSpace(document.PluginPath) || !string.IsNullOrWhiteSpace(document.TypeName))
        {
            name = CustomMutationPluginLoader.LoadBitMutation(
                ResolvePluginPath(baseDirectory, document.PluginPath),
                document.TypeName ?? throw new InvalidOperationException("customMutation.typeName is required when pluginPath is specified."),
                name);
        }

        return new CustomMutationParameters(
            name ?? throw new InvalidOperationException("customMutation.name is required."),
            ParseRequiredEnum<CustomMutationPosition>(document.Position, nameof(document.Position)),
            document.Parameters ?? new Dictionary<string, string>(StringComparer.Ordinal));
    }

    private static CustomChunkMutationParameters? ToCustomChunkMutation(CustomChunkMutationParametersDocument? document, string baseDirectory)
    {
        if (document is null)
        {
            return null;
        }

        string? name = document.Name;
        if (!string.IsNullOrWhiteSpace(document.PluginPath) || !string.IsNullOrWhiteSpace(document.TypeName))
        {
            name = CustomMutationPluginLoader.LoadChunkMutation(
                ResolvePluginPath(baseDirectory, document.PluginPath),
                document.TypeName ?? throw new InvalidOperationException("customChunkMutation.typeName is required when pluginPath is specified."),
                name);
        }

        return new CustomChunkMutationParameters(
            name ?? throw new InvalidOperationException("customChunkMutation.name is required."),
            ParseRequiredEnum<CustomChunkMutationPosition>(document.Position, nameof(document.Position)),
            document.Parameters ?? new Dictionary<string, string>(StringComparer.Ordinal));
    }

    private static string ResolvePluginPath(string baseDirectory, string? pluginPath)
    {
        if (string.IsNullOrWhiteSpace(pluginPath))
        {
            throw new InvalidOperationException("Custom mutation pluginPath is required when typeName is specified.");
        }

        return Path.IsPathRooted(pluginPath)
            ? pluginPath
            : Path.GetFullPath(Path.Combine(baseDirectory, pluginPath));
    }

    private static ulong ResolveSaltSeed(ulong? saltSeed, string? saltText)
    {
        if (saltSeed is not null && saltText is not null)
        {
            throw new InvalidOperationException("Config parameters must specify at most one of saltSeed or saltText.");
        }

        return saltText is not null
            ? SaltDerivation.DeriveSaltSeedFromText(saltText)
            : saltSeed ?? 0UL;
    }

    private static TEnum ParseRequiredEnum<TEnum>(string? value, string fieldName) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{fieldName} is required.");
        }

        return ParseEnum<TEnum>(value, fieldName);
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue) where TEnum : struct
    {
        return string.IsNullOrWhiteSpace(value) ? defaultValue : ParseEnum<TEnum>(value, typeof(TEnum).Name);
    }

    private static TEnum? ParseNullableEnum<TEnum>(string? value) where TEnum : struct
    {
        return string.IsNullOrWhiteSpace(value) ? null : ParseEnum<TEnum>(value, typeof(TEnum).Name);
    }

    private static TEnum ParseEnum<TEnum>(string value, string fieldName) where TEnum : struct
    {
        if (Enum.TryParse<TEnum>(value, true, out TEnum parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Unsupported value '{value}' for {fieldName}.");
    }

    private static ValueRangeKind InferValueRange(IReadOnlyList<ulong> values)
    {
        ulong max = values.Count == 0 ? 0UL : values.Max();
        return max switch
        {
            <= 2UL => ValueRangeKind.Tiny,
            <= 10_000UL => ValueRangeKind.Small,
            _ => ValueRangeKind.Large
        };
    }

    private static double GetValueRangeWeight(ValueRangeKind kind)
    {
        return kind switch
        {
            ValueRangeKind.Tiny => 0.50,
            ValueRangeKind.Small => 1.00,
            ValueRangeKind.Large => 1.00,
            _ => 1.00
        };
    }
}

public sealed record LoadedBenchmarkConfig(
    IReadOnlyList<BenchmarkScenario> Scenarios,
    BenchmarkExecutionOptions Options,
    string? OutputMarkdown,
    string? OutputCsv,
    bool IncludeInvalid,
    BenchmarkValueSetKind ValueSet,
    int Top);

internal sealed record BenchmarkConfigDocument(
    [property: JsonPropertyName("scenarios")] List<BenchmarkScenarioDocument>? Scenarios,
    [property: JsonPropertyName("benchmark")] BenchmarkExecutionDocument? Benchmark);

internal sealed record BenchmarkScenarioDocument(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("values")] List<ulong>? Values,
    [property: JsonPropertyName("parameters")] CodecParametersDocument? Parameters);

internal sealed record BenchmarkExecutionDocument(
    [property: JsonPropertyName("mode")] string? Mode = null,
    [property: JsonPropertyName("iterations")] int? Iterations = null,
    [property: JsonPropertyName("validate")] bool? Validate = null,
    [property: JsonPropertyName("outputMarkdown")] string? OutputMarkdown = null,
    [property: JsonPropertyName("outputCsv")] string? OutputCsv = null,
    [property: JsonPropertyName("top")] int? Top = null,
    [property: JsonPropertyName("includeInvalid")] bool? IncludeInvalid = null,
    [property: JsonPropertyName("valueSet")] string? ValueSet = null);

internal sealed record CodecParametersDocument(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("numberKind")] string? NumberKind,
    [property: JsonPropertyName("bitLength")] int? BitLength,
    [property: JsonPropertyName("saltSeed")] ulong? SaltSeed,
    [property: JsonPropertyName("saltText")] string? SaltText,
    [property: JsonPropertyName("binary")] BinaryParametersDocument? Binary,
    [property: JsonPropertyName("mixer")] MixerParametersDocument? Mixer,
    [property: JsonPropertyName("permutation")] PermutationParametersDocument? Permutation,
    [property: JsonPropertyName("chunking")] ChunkingParametersDocument? Chunking,
    [property: JsonPropertyName("emitter")] EmitterParametersDocument? Emitter,
    [property: JsonPropertyName("customMutation")] CustomMutationParametersDocument? CustomMutation,
    [property: JsonPropertyName("customChunkMutation")] CustomChunkMutationParametersDocument? CustomChunkMutation);

internal sealed record BinaryParametersDocument(
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("bitOrder")] string? BitOrder,
    [property: JsonPropertyName("byteOrder")] string? ByteOrder);

internal sealed record MixerParametersDocument(
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("maskDerivation")] string? MaskDerivation,
    [property: JsonPropertyName("literalMask")] ulong? LiteralMask,
    [property: JsonPropertyName("literalAddend")] ulong? LiteralAddend,
    [property: JsonPropertyName("multiplier")] ulong? Multiplier);

internal sealed record PermutationParametersDocument(
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("rotateBy")] int? RotateBy,
    [property: JsonPropertyName("nibbleSwap")] string? NibbleSwap,
    [property: JsonPropertyName("chunkPermutationGroupSize")] int? ChunkPermutationGroupSize,
    [property: JsonPropertyName("chunkPermutationVariant")] string? ChunkPermutationVariant,
    [property: JsonPropertyName("chunkPermutationOrder")] List<int>? ChunkPermutationOrder,
    [property: JsonPropertyName("chunkPermutationRotateBy")] int? ChunkPermutationRotateBy,
    [property: JsonPropertyName("feistelRounds")] int? FeistelRounds,
    [property: JsonPropertyName("feistelRoundFunction")] string? FeistelRoundFunction);

internal sealed record ChunkingParametersDocument(
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("chunkSize")] int? ChunkSize,
    [property: JsonPropertyName("chunkReadOrder")] string? ChunkReadOrder);

internal sealed record EmitterParametersDocument(
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("alphabetKind")] string? AlphabetKind,
    [property: JsonPropertyName("outputKind")] string? OutputKind,
    [property: JsonPropertyName("customAlphabet")] string? CustomAlphabet,
    [property: JsonPropertyName("byteArrayTextFormat")] string? ByteArrayTextFormat);

internal sealed record CustomMutationParametersDocument(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("position")] string? Position,
    [property: JsonPropertyName("parameters")] Dictionary<string, string>? Parameters,
    [property: JsonPropertyName("pluginPath")] string? PluginPath,
    [property: JsonPropertyName("typeName")] string? TypeName);

internal sealed record CustomChunkMutationParametersDocument(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("position")] string? Position,
    [property: JsonPropertyName("parameters")] Dictionary<string, string>? Parameters,
    [property: JsonPropertyName("pluginPath")] string? PluginPath,
    [property: JsonPropertyName("typeName")] string? TypeName);
