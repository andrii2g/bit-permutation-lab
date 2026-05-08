using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using A2G.BitPermutationLab.Benchmarking;
using A2G.BitPermutationLab.Binary;
using A2G.BitPermutationLab.Chunking;
using A2G.BitPermutationLab.Core;
using A2G.BitPermutationLab.Custom;
using A2G.BitPermutationLab.Emitters;
using A2G.BitPermutationLab.Mixers;
using A2G.BitPermutationLab.Permutations;

namespace A2G.BitPermutationLab.Cli;

public static class CliApplication
{
    private static readonly CodecPipeline Pipeline = new();

    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        try
        {
            if (args.Length == 0)
            {
                PrintUsage(stderr);
                return 1;
            }

            string command = args[0].ToLowerInvariant();
            CliArguments arguments = CliArguments.Parse(args[1..]);

            return command switch
            {
                "encode" => RunEncode(arguments, stdout, stderr),
                "decode" => RunDecode(arguments, stdout, stderr),
                "list" => RunList(stdout),
                "benchmark" => RunBenchmark(arguments, stdout, stderr),
                _ => Fail(stderr, $"Unknown command '{command}'.")
            };
        }
        catch (CliUsageException exception)
        {
            stderr.WriteLine(exception.Message);
            return 1;
        }
        catch (Exception exception)
        {
            stderr.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunEncode(CliArguments arguments, TextWriter stdout, TextWriter stderr)
    {
        ulong value = arguments.GetRequiredULong("value");
        CodecParameters parameters = CreateParameters(arguments);
        CodecResult result = Pipeline.Encode(value, parameters);

        switch (result.OutputKind)
        {
            case OutputKind.String:
                stdout.WriteLine($"Encoded: {result.StringValue}");
                return 0;
            case OutputKind.CharArray:
                stdout.WriteLine($"Encoded: {new string(result.CharArrayValue ?? Array.Empty<char>())}");
                return 0;
            case OutputKind.ByteArray:
                stdout.WriteLine($"EncodedBytes{parameters.Emitter.ByteArrayTextFormat}: {FormatByteArray(result.ByteArrayValue ?? Array.Empty<byte>(), parameters.Emitter.ByteArrayTextFormat)}");
                return 0;
            default:
                return Fail(stderr, $"Unsupported output kind '{result.OutputKind}'.");
        }
    }

    private static int RunDecode(CliArguments arguments, TextWriter stdout, TextWriter stderr)
    {
        CodecParameters parameters = CreateParameters(arguments);
        DecodeResult result = parameters.Emitter.OutputKind == OutputKind.ByteArray
            ? Pipeline.Decode(ParseByteArrayInput(arguments.GetRequiredString("value"), parameters.Emitter.ByteArrayTextFormat), parameters)
            : Pipeline.Decode(arguments.GetRequiredString("value").AsSpan(), parameters);

        if (!result.Success)
        {
            return Fail(stderr, result.ErrorMessage ?? "Decode failed.");
        }

        stdout.WriteLine($"Decoded: {result.Value}");
        return 0;
    }

    private static int RunList(TextWriter stdout)
    {
        stdout.WriteLine("Available scenario components");
        stdout.WriteLine("Number kinds: UInt32, UInt64");
        stdout.WriteLine("Bit lengths: 8, 16, 24, 32, 40, 48, 56, 64");
        stdout.WriteLine("Binary kinds: FixedUnsigned");
        stdout.WriteLine("Bit orders: MsbFirst, LsbFirst");
        stdout.WriteLine("Byte orders: BigEndian, LittleEndian");
        stdout.WriteLine("Mixers: None, Xor, Add, Multiply");
        stdout.WriteLine("Mixer derivation modes: None, UseSaltSeedDirectly, SplitMix64");
        stdout.WriteLine("Permutations: Identity, Not, Rotate, ByteSwap, BitReverse, NibbleSwap, ChunkPermutation, Feistel");
        stdout.WriteLine("Nibble swap variants: ReverseNibbles, SwapAdjacentNibbles");
        stdout.WriteLine("Chunk permutation variants: ExplicitOrder, ReverseGroups, RotateGroupsLeft, RotateGroupsRight, SwapAdjacentGroups, SaltShuffle");
        stdout.WriteLine("Feistel round functions: XorShiftAdd, MultiplyXor");
        stdout.WriteLine("Chunk sizes: 4, 5, 6, 8");
        stdout.WriteLine("Chunk read orders: MsbFirst, LsbFirst");
        stdout.WriteLine("Emitters: Hex16, Base32Crockford, Base64Url, ByteArray, CustomAlphabet");
        stdout.WriteLine("Alphabets: None, Hex16, Base32Crockford, Base64Url, Custom");
        stdout.WriteLine("Output kinds: String, CharArray, ByteArray");
        stdout.WriteLine("Byte array text formats: Hex, Base64, CommaSeparatedDecimal");
        stdout.WriteLine("Benchmark profiles: Quick, Default, Full");
        stdout.WriteLine("Benchmark weighting profiles: Smoke, SpeedFirst, Balanced, Exploratory, Exhaustive");
        stdout.WriteLine($"Registered custom mutations: {FormatNames(CustomMutationRegistry.GetRegisteredMutationNames())}");
        stdout.WriteLine($"Registered custom chunk mutations: {FormatNames(CustomMutationRegistry.GetRegisteredChunkMutationNames())}");
        stdout.WriteLine("Direct CLI supports the built-in binary, mixer, permutation, chunking, emitter, and custom-mutation parameter surface.");
        stdout.WriteLine("Custom mutation plugins can be loaded with --custom-mutation-plugin/--custom-mutation-type and --custom-chunk-mutation-plugin/--custom-chunk-mutation-type.");
        return 0;
    }

    private static int RunBenchmark(CliArguments arguments, TextWriter stdout, TextWriter stderr)
    {
        BenchmarkModeKind modeKind = ParseBenchmarkModeKind(arguments.GetOptionalString("mode") ?? "quick");
        if (modeKind == BenchmarkModeKind.BenchmarkDotNet)
        {
            return RunBenchmarkDotNet(arguments, stdout, stderr);
        }

        WeightingOverrides overrides = LoadWeightingOverrides(arguments);
        BenchmarkReportOptions report = CreateReportOptions(arguments, overrides.IncludeRawUnweightedReport ?? true);
        bool includeInvalid = arguments.GetOptionalBool("include-invalid") ?? true;
        int top = arguments.GetOptionalInt("top") ?? 5;

        if (arguments.Contains("config"))
        {
            LoadedBenchmarkConfig loaded = BenchmarkConfigLoader.Load(arguments.GetRequiredString("config"), arguments.GetOptionalString("weights-config"));
            BenchmarkExecutionOptions execution = loaded.Options with
            {
                Iterations = arguments.GetOptionalInt("iterations") ?? loaded.Options.Iterations,
                Report = CreateReportOptions(arguments, loaded.Options.Report.IncludeUnweightedReport),
                ValidateScenarios = arguments.GetOptionalBool("validate") ?? loaded.Options.ValidateScenarios
            };

            IReadOnlyList<BenchmarkScenario> scenarios = ApplyValueSet(
                loaded.Scenarios,
                arguments.GetOptionalString("value-set"),
                loaded.ValueSet);

            BenchmarkRunResult configResult = BenchmarkRunner.RunDetailed(scenarios, execution);
            WriteBenchmarkHeader(stdout, execution.ProfileLabel, execution.ModeLabel, execution.Iterations, execution.Selection, top);
            WriteBenchmarkOutputs(configResult, stdout, arguments.GetOptionalString("output") ?? loaded.OutputMarkdown, arguments.GetOptionalString("csv") ?? loaded.OutputCsv, includeInvalid, top);
            return 0;
        }

        if (HasScenarioArguments(arguments) && !arguments.Contains("profile"))
        {
            return RunSingleScenarioBenchmark(arguments, stdout);
        }

        BenchmarkProfileKind profileKind = arguments.GetEnum("profile", BenchmarkProfileKind.Quick, ParseBenchmarkProfileKind);
        BenchmarkSelectionOptions profileDefaults = BenchmarkProfileFactory.CreateSelectionOptions(profileKind);
        int iterations = arguments.GetOptionalInt("iterations") ?? 1000;
        if (iterations <= 0)
        {
            throw new CliUsageException("--iterations must be greater than zero.");
        }

        WeightingProfileKind weightingProfile = arguments.GetEnum("weighting-profile", DefaultWeightingProfile(profileKind), ParseWeightingProfileKind);
        int? scenarioBudget = arguments.GetOptionalInt("scenario-budget") ?? profileDefaults.ScenarioBudget;
        ulong samplingSeed = arguments.GetOptionalULong("sampling-seed") ?? profileDefaults.SamplingSeed;
        bool includeRequiredBaselines = arguments.GetOptionalBool("include-required-baselines") ?? true;

        BenchmarkSelectionOptions selection = BenchmarkProfileFactory.ApplySelectionOverrides(
            new BenchmarkSelectionOptions(weightingProfile, scenarioBudget, samplingSeed, includeRequiredBaselines),
            overrides);

        BenchmarkExecutionOptions directExecution = new(
            profileKind.ToString(),
            BenchmarkModeKind.Quick.ToString(),
            iterations,
            selection,
            report,
            arguments.GetOptionalBool("validate") ?? true);

        IReadOnlyList<BenchmarkScenario> directScenarios = ApplyValueSet(
            BenchmarkProfileFactory.Create(directExecution.Selection, overrides),
            arguments.GetOptionalString("value-set"),
            BenchmarkValueSetKind.Default);

        BenchmarkRunResult result = BenchmarkRunner.RunDetailed(directScenarios, directExecution);
        WriteBenchmarkHeader(stdout, profileKind.ToString(), BenchmarkModeKind.Quick.ToString(), iterations, directExecution.Selection, top);
        WriteBenchmarkOutputs(result, stdout, arguments.GetOptionalString("output"), arguments.GetOptionalString("csv"), includeInvalid, top);
        return 0;
    }

    private static int RunSingleScenarioBenchmark(CliArguments arguments, TextWriter stdout)
    {
        int iterations = arguments.GetOptionalInt("iterations") ?? 1000;
        int top = arguments.GetOptionalInt("top") ?? 5;
        WeightingOverrides overrides = LoadWeightingOverrides(arguments);
        CodecParameters parameters = CreateParameters(arguments);
        IReadOnlyList<ulong> values = GetDirectBenchmarkValues(arguments, parameters.BitLength);
        BenchmarkScenario scenario = new(
            $"{parameters.Name}:direct",
            parameters.Name,
            parameters,
            ParameterTierKind.Explicit,
            InferRangeKind(values),
            values,
            new ScenarioWeights(1.0, 1.0, 1.0, 1.0, parameters.CustomMutation is null && parameters.CustomChunkMutation is null ? 1.0 : 1.2, 1.0, 1.0, false));

        BenchmarkExecutionOptions execution = new(
            "Direct",
            BenchmarkModeKind.Quick.ToString(),
            iterations,
            new BenchmarkSelectionOptions(WeightingProfileKind.Balanced, 1, 0UL, true),
            CreateReportOptions(arguments, overrides.IncludeRawUnweightedReport ?? true),
            arguments.GetOptionalBool("validate") ?? true);

        BenchmarkRunResult result = BenchmarkRunner.RunDetailed([scenario], execution);
        WriteBenchmarkHeader(stdout, "Direct", BenchmarkModeKind.Quick.ToString(), iterations, execution.Selection, top);
        WriteBenchmarkOutputs(result, stdout, arguments.GetOptionalString("output"), arguments.GetOptionalString("csv"), arguments.GetOptionalBool("include-invalid") ?? true, top);
        return 0;
    }

    private static int RunBenchmarkDotNet(CliArguments arguments, TextWriter stdout, TextWriter stderr)
    {
        string benchmarkDll = ResolveBenchmarkProjectDll();
        if (!File.Exists(benchmarkDll))
        {
            return Fail(stderr, $"BenchmarkDotNet runner assembly was not found at '{benchmarkDll}'. Build the benchmark project first.");
        }

        string? temporaryConfigPath = null;
        List<string> forwardedArguments = [QuoteArgument(benchmarkDll)];
        AppendForwardedArgument(forwardedArguments, "mode", "benchmarkdotnet");

        if (arguments.Contains("config"))
        {
            AppendForwardedArgument(forwardedArguments, "config", arguments.GetRequiredString("config"));
        }
        else if (HasScenarioArguments(arguments) && !arguments.Contains("profile"))
        {
            temporaryConfigPath = CreateTemporaryBenchmarkConfig(arguments);
            AppendForwardedArgument(forwardedArguments, "config", temporaryConfigPath);
        }
        else
        {
            AppendForwardedArgument(forwardedArguments, "profile", arguments.GetOptionalString("profile"));
        }

        AppendForwardedArgument(forwardedArguments, "iterations", arguments.GetOptionalString("iterations"));
        AppendForwardedArgument(forwardedArguments, "weighting-profile", arguments.GetOptionalString("weighting-profile"));
        AppendForwardedArgument(forwardedArguments, "scenario-budget", arguments.GetOptionalString("scenario-budget"));
        AppendForwardedArgument(forwardedArguments, "sampling-seed", arguments.GetOptionalString("sampling-seed"));
        AppendForwardedArgument(forwardedArguments, "weights-config", arguments.GetOptionalString("weights-config"));
        AppendForwardedArgument(forwardedArguments, "include-required-baselines", arguments.GetOptionalString("include-required-baselines"));
        AppendForwardedArgument(forwardedArguments, "report-weighted", arguments.GetOptionalString("report-weighted"));
        AppendForwardedArgument(forwardedArguments, "report-unweighted", arguments.GetOptionalString("report-unweighted"));
        AppendForwardedArgument(forwardedArguments, "validate", arguments.GetOptionalString("validate"));
        AppendForwardedArgument(forwardedArguments, "top", arguments.GetOptionalString("top"));
        AppendForwardedArgument(forwardedArguments, "output", arguments.GetOptionalString("output"));
        AppendForwardedArgument(forwardedArguments, "csv", arguments.GetOptionalString("csv"));

        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            Arguments = string.Join(" ", forwardedArguments),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using Process process = Process.Start(startInfo)
                ?? throw new CliUsageException("Failed to start BenchmarkDotNet runner process.");

            string childStdout = process.StandardOutput.ReadToEnd();
            string childStderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(childStdout))
            {
                stdout.Write(childStdout);
            }

            if (!string.IsNullOrEmpty(childStderr))
            {
                stderr.Write(childStderr);
            }

            return process.ExitCode;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(temporaryConfigPath) && File.Exists(temporaryConfigPath))
            {
                File.Delete(temporaryConfigPath);
            }
        }
    }

    private static BenchmarkReportOptions CreateReportOptions(CliArguments arguments, bool defaultIncludeUnweightedReport)
    {
        bool includeWeightedReport = arguments.GetOptionalBool("report-weighted") ?? true;
        bool includeUnweightedReport = arguments.GetOptionalBool("report-unweighted") ?? defaultIncludeUnweightedReport;
        if (!includeWeightedReport && !includeUnweightedReport)
        {
            throw new CliUsageException("At least one of --report-weighted or --report-unweighted must be true.");
        }

        return new BenchmarkReportOptions(includeWeightedReport, includeUnweightedReport);
    }

    private static WeightingOverrides LoadWeightingOverrides(CliArguments arguments)
    {
        string? path = arguments.GetOptionalString("weights-config");
        return string.IsNullOrWhiteSpace(path)
            ? WeightingOverrides.Empty
            : BenchmarkWeightingConfigLoader.Load(path);
    }

    private static IReadOnlyList<BenchmarkScenario> ApplyValueSet(
        IReadOnlyList<BenchmarkScenario> scenarios,
        string? explicitValueSet,
        BenchmarkValueSetKind defaultValueSet)
    {
        BenchmarkValueSetKind valueSet = explicitValueSet is null
            ? defaultValueSet
            : ParseBenchmarkValueSetKind(explicitValueSet);

        IReadOnlyList<ulong>? values = valueSet switch
        {
            BenchmarkValueSetKind.Default => null,
            BenchmarkValueSetKind.Small => [1UL, 2UL],
            BenchmarkValueSetKind.Middle => [1_000UL, 10_000UL],
            BenchmarkValueSetKind.Large => [1_000_000UL, 1_000_000_000UL],
            BenchmarkValueSetKind.Max => [ulong.MaxValue, ulong.MaxValue - 1UL, uint.MaxValue],
            _ => null
        };

        if (values is null)
        {
            return scenarios;
        }

        return [.. scenarios.Select(scenario =>
            scenario with
            {
                Values = [.. values.Where(value => (value & ~BitMask.ForBitLength(scenario.Parameters.BitLength)) == 0)]
            })];
    }

    private static IReadOnlyList<ulong> GetDirectBenchmarkValues(CliArguments arguments, int bitLength)
    {
        if (arguments.TryGetValue("value", out string? explicitValue))
        {
            return [ParseULongInvariant(explicitValue!, "value")];
        }

        BenchmarkValueSetKind kind = arguments.GetEnum("value-set", BenchmarkValueSetKind.Default, ParseBenchmarkValueSetKind);
        IReadOnlyList<ulong> values = kind switch
        {
            BenchmarkValueSetKind.Default => [1UL, 2UL, 1_000UL, 10_000UL, 1_000_000UL, 1_000_000_000UL],
            BenchmarkValueSetKind.Small => [1UL, 2UL],
            BenchmarkValueSetKind.Middle => [1_000UL, 10_000UL],
            BenchmarkValueSetKind.Large => [1_000_000UL, 1_000_000_000UL],
            BenchmarkValueSetKind.Max => [ulong.MaxValue, ulong.MaxValue - 1UL, uint.MaxValue],
            _ => [1UL, 2UL]
        };

        ulong mask = BitMask.ForBitLength(bitLength);
        return [.. values.Where(value => (value & ~mask) == 0)];
    }

    private static ValueRangeKind InferRangeKind(IReadOnlyList<ulong> values)
    {
        ulong max = values.Count == 0 ? 0UL : values.Max();
        return max switch
        {
            <= 2UL => ValueRangeKind.Tiny,
            <= 10_000UL => ValueRangeKind.Small,
            _ => ValueRangeKind.Large
        };
    }

    private static void WriteBenchmarkHeader(
        TextWriter stdout,
        string profile,
        string mode,
        int iterations,
        BenchmarkSelectionOptions selection,
        int top)
    {
        stdout.WriteLine($"Profile: {profile}");
        stdout.WriteLine($"Mode: {mode}");
        stdout.WriteLine($"WeightingProfile: {selection.WeightingProfile}");
        stdout.WriteLine($"Iterations: {iterations}");
        if (selection.ScenarioBudget is not null)
        {
            stdout.WriteLine($"ScenarioBudget: {selection.ScenarioBudget}");
        }

        stdout.WriteLine($"SamplingSeed: {selection.SamplingSeed}");
        stdout.WriteLine($"IncludeRequiredBaselines: {selection.IncludeRequiredBaselines}");
        stdout.WriteLine($"Top: {top}");
    }

    private static void WriteBenchmarkOutputs(
        BenchmarkRunResult result,
        TextWriter stdout,
        string? markdownPath,
        string? csvPath,
        bool includeInvalid,
        int top)
    {
        BenchmarkRunResult effectiveResult = includeInvalid
            ? result
            : result with { SkippedRows = [] };

        BenchmarkConsoleFormatter.Write(effectiveResult.Rows, stdout, effectiveResult.Options.Report);

        if (!string.IsNullOrWhiteSpace(markdownPath))
        {
            EnsureParentDirectory(markdownPath);
            using StreamWriter writer = File.CreateText(markdownPath);
            MarkdownBenchmarkReportWriter.Write(effectiveResult, writer, top);
            stdout.WriteLine($"MarkdownReport: {markdownPath}");
        }

        if (!string.IsNullOrWhiteSpace(csvPath))
        {
            EnsureParentDirectory(csvPath);
            using StreamWriter writer = File.CreateText(csvPath);
            CsvBenchmarkReportWriter.Write(effectiveResult, writer);
            stdout.WriteLine($"CsvReport: {csvPath}");
        }
    }

    private static CodecParameters CreateParameters(CliArguments arguments)
    {
        ulong saltSeed = ResolveSaltSeed(arguments);
        EmitterKind emitterKind = arguments.GetEnum("emitter", EmitterKind.Hex16, ParseEmitterKind);
        PermutationKind permutationKind = arguments.GetEnum("permute", PermutationKind.Identity, ParsePermutationKind);

        BinaryParameters binary = new(
            arguments.GetEnum("bin", BinaryKind.FixedUnsigned, ParseBinaryKind),
            arguments.GetEnum("bit-order", BitOrderKind.MsbFirst, ParseBitOrderKind),
            arguments.GetEnum("byte-order", ByteOrderKind.BigEndian, ParseByteOrderKind));

        MixerParameters mixer = new(
            arguments.GetEnum("mix", MixerKind.None, ParseMixerKind),
            arguments.GetEnum("mask-derivation", SaltDerivationKind.SplitMix64, ParseSaltDerivationKind),
            arguments.GetOptionalULong("xor-mask"),
            arguments.GetOptionalULong("addend"),
            arguments.GetOptionalULong("multiplier"));

        PermutationParameters permutation = new(
            permutationKind,
            arguments.GetOptionalInt("rotate-by") ?? (permutationKind == PermutationKind.Rotate ? 11 : null),
            ParseOptionalNibbleSwap(arguments.GetOptionalString("nibble-swap"), permutationKind),
            arguments.GetOptionalInt("chunk-permute-group-size"),
            ParseOptionalChunkPermutationVariant(arguments.GetOptionalString("chunk-permute-variant")),
            ParseCsvIntegers(arguments.GetOptionalString("chunk-permute-order")),
            arguments.GetOptionalInt("chunk-permute-rotate-by"),
            arguments.GetOptionalInt("feistel-rounds"),
            ParseOptionalFeistelRoundFunction(arguments.GetOptionalString("feistel-round-function")));

        ChunkingParameters chunking = new(
            arguments.GetEnum("chunker", ChunkerKind.Fixed, ParseChunkerKind),
            arguments.GetRequiredInt("chunk-size"),
            arguments.GetEnum("chunk-read-order", BitOrderKind.MsbFirst, ParseBitOrderKind));

        EmitterParameters emitter = new(
            emitterKind,
            arguments.GetEnum("alphabet", DefaultAlphabetKind(emitterKind), ParseAlphabetKind),
            arguments.GetEnum("output-kind", DefaultOutputKind(emitterKind), ParseOutputKind),
            arguments.GetOptionalString("custom-alphabet"),
            arguments.GetEnum("byte-array-format", ByteArrayTextFormat.Hex, ParseByteArrayTextFormat));

        return new CodecParameters(
            arguments.GetOptionalString("scenario-name") ?? "cli-scenario",
            arguments.GetEnum("number-kind", NumberKind.UInt64, ParseNumberKind),
            arguments.GetRequiredInt("bits"),
            saltSeed,
            binary,
            mixer,
            permutation,
            chunking,
            emitter,
            CreateCustomMutation(arguments),
            CreateCustomChunkMutation(arguments));
    }

    private static CustomMutationParameters? CreateCustomMutation(CliArguments arguments)
    {
        string? name = arguments.GetOptionalString("custom-mutation-name");
        string? pluginPath = arguments.GetOptionalString("custom-mutation-plugin");
        string? typeName = arguments.GetOptionalString("custom-mutation-type");

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(pluginPath) && string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(pluginPath) || !string.IsNullOrWhiteSpace(typeName))
        {
            if (string.IsNullOrWhiteSpace(pluginPath) || string.IsNullOrWhiteSpace(typeName))
            {
                throw new CliUsageException("--custom-mutation-plugin and --custom-mutation-type must be provided together.");
            }

            name = CustomMutationPluginLoader.LoadBitMutation(pluginPath, typeName, name);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new CliUsageException("A custom bit-value mutation requires --custom-mutation-name or a loadable plugin type.");
        }

        return new CustomMutationParameters(
            name,
            arguments.GetEnum("custom-mutation-position", CustomMutationPosition.AfterMix, ParseCustomMutationPosition),
            ParseKeyValuePairs(arguments.GetAllStrings("custom-mutation-param")));
    }

    private static CustomChunkMutationParameters? CreateCustomChunkMutation(CliArguments arguments)
    {
        string? name = arguments.GetOptionalString("custom-chunk-mutation-name");
        string? pluginPath = arguments.GetOptionalString("custom-chunk-mutation-plugin");
        string? typeName = arguments.GetOptionalString("custom-chunk-mutation-type");

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(pluginPath) && string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(pluginPath) || !string.IsNullOrWhiteSpace(typeName))
        {
            if (string.IsNullOrWhiteSpace(pluginPath) || string.IsNullOrWhiteSpace(typeName))
            {
                throw new CliUsageException("--custom-chunk-mutation-plugin and --custom-chunk-mutation-type must be provided together.");
            }

            name = CustomMutationPluginLoader.LoadChunkMutation(pluginPath, typeName, name);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new CliUsageException("A custom chunk mutation requires --custom-chunk-mutation-name or a loadable plugin type.");
        }

        return new CustomChunkMutationParameters(
            name,
            arguments.GetEnum("custom-chunk-mutation-position", CustomChunkMutationPosition.BeforeEmit, ParseCustomChunkMutationPosition),
            ParseKeyValuePairs(arguments.GetAllStrings("custom-chunk-mutation-param")));
    }

    private static Dictionary<string, string> ParseKeyValuePairs(IReadOnlyList<string> values)
    {
        Dictionary<string, string> parsed = new(StringComparer.Ordinal);
        foreach (string value in values)
        {
            int separatorIndex = value.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex == value.Length - 1)
            {
                throw new CliUsageException($"Custom mutation parameter '{value}' must use key=value format.");
            }

            parsed[value[..separatorIndex]] = value[(separatorIndex + 1)..];
        }

        return parsed;
    }

    private static IReadOnlyList<int>? ParseCsvIntegers(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static part => int.Parse(part, CultureInfo.InvariantCulture))];
    }

    private static bool HasScenarioArguments(CliArguments arguments)
    {
        return arguments.Contains("bits") || arguments.Contains("mix") || arguments.Contains("permute") || arguments.Contains("emitter");
    }

    private static ulong ResolveSaltSeed(CliArguments arguments)
    {
        bool hasSalt = arguments.TryGetValue("salt", out string? saltValue);
        bool hasSaltText = arguments.TryGetValue("salt-text", out string? saltTextValue);

        if (hasSalt && hasSaltText)
        {
            throw new CliUsageException("--salt and --salt-text are mutually exclusive.");
        }

        if (hasSalt)
        {
            return ParseULongInvariant(saltValue!, "salt");
        }

        if (hasSaltText)
        {
            return SaltDerivation.DeriveSaltSeedFromText(saltTextValue!);
        }

        return 0UL;
    }

    private static string FormatByteArray(byte[] bytes, ByteArrayTextFormat format)
    {
        return format switch
        {
            ByteArrayTextFormat.Hex => Convert.ToHexString(bytes),
            ByteArrayTextFormat.Base64 => Convert.ToBase64String(bytes),
            ByteArrayTextFormat.CommaSeparatedDecimal => string.Join(",", bytes.Select(static b => b.ToString(CultureInfo.InvariantCulture))),
            _ => throw new CliUsageException($"Unsupported byte-array format '{format}'.")
        };
    }

    private static byte[] ParseByteArrayInput(string value, ByteArrayTextFormat format)
    {
        return format switch
        {
            ByteArrayTextFormat.Hex => Convert.FromHexString(value),
            ByteArrayTextFormat.Base64 => Convert.FromBase64String(value),
            ByteArrayTextFormat.CommaSeparatedDecimal => value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(static part => byte.Parse(part, CultureInfo.InvariantCulture))
                .ToArray(),
            _ => throw new CliUsageException($"Unsupported byte-array format '{format}'.")
        };
    }

    private static string ResolveBenchmarkProjectDll()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "benchmarks", "A2G.BitPermutationLab.Benchmarks", "bin", "Debug", "net10.0", "A2G.BitPermutationLab.Benchmarks.dll"));
    }

    private static string CreateTemporaryBenchmarkConfig(CliArguments arguments)
    {
        CodecParameters parameters = CreateParameters(arguments);
        IReadOnlyList<ulong> values = GetDirectBenchmarkValues(arguments, parameters.BitLength);

        var configDocument = new
        {
            scenarios = new object[]
            {
                new
                {
                    name = parameters.Name,
                    values,
                    parameters = new
                    {
                        name = parameters.Name,
                        numberKind = parameters.NumberKind.ToString(),
                        bitLength = parameters.BitLength,
                        saltSeed = parameters.SaltSeed,
                        binary = new
                        {
                            kind = parameters.Binary.Kind.ToString(),
                            bitOrder = parameters.Binary.BitOrder.ToString(),
                            byteOrder = parameters.Binary.ByteOrder.ToString()
                        },
                        mixer = new
                        {
                            kind = parameters.Mixer.Kind.ToString(),
                            maskDerivation = parameters.Mixer.MaskDerivation.ToString(),
                            literalMask = parameters.Mixer.LiteralMask,
                            literalAddend = parameters.Mixer.LiteralAddend,
                            multiplier = parameters.Mixer.Multiplier
                        },
                        permutation = new
                        {
                            kind = parameters.Permutation.Kind.ToString(),
                            rotateBy = parameters.Permutation.RotateBy,
                            nibbleSwap = parameters.Permutation.NibbleSwap?.ToString(),
                            chunkPermutationGroupSize = parameters.Permutation.ChunkPermutationGroupSize,
                            chunkPermutationVariant = parameters.Permutation.ChunkPermutationVariant?.ToString(),
                            chunkPermutationOrder = parameters.Permutation.ChunkPermutationOrder,
                            chunkPermutationRotateBy = parameters.Permutation.ChunkPermutationRotateBy,
                            feistelRounds = parameters.Permutation.FeistelRounds,
                            feistelRoundFunction = parameters.Permutation.FeistelRoundFunction?.ToString()
                        },
                        chunking = new
                        {
                            kind = parameters.Chunking.Kind.ToString(),
                            chunkSize = parameters.Chunking.ChunkSize,
                            chunkReadOrder = parameters.Chunking.ChunkReadOrder.ToString()
                        },
                        emitter = new
                        {
                            kind = parameters.Emitter.Kind.ToString(),
                            alphabetKind = parameters.Emitter.AlphabetKind.ToString(),
                            outputKind = parameters.Emitter.OutputKind.ToString(),
                            customAlphabet = parameters.Emitter.CustomAlphabet,
                            byteArrayTextFormat = parameters.Emitter.ByteArrayTextFormat.ToString()
                        },
                        customMutation = CreateCustomMutationConfigDocument(arguments, parameters.CustomMutation),
                        customChunkMutation = CreateCustomChunkMutationConfigDocument(arguments, parameters.CustomChunkMutation)
                    }
                }
            },
            benchmark = new
            {
                mode = BenchmarkModeKind.BenchmarkDotNet.ToString(),
                iterations = arguments.GetOptionalInt("iterations") ?? 1000,
                validate = arguments.GetOptionalBool("validate") ?? true
            }
        };

        string path = Path.Combine(Path.GetTempPath(), $"bit-permutation-lab-bdn-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(configDocument, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private static object? CreateCustomMutationConfigDocument(CliArguments arguments, CustomMutationParameters? mutation)
    {
        if (mutation is null)
        {
            return null;
        }

        return new
        {
            name = mutation.Name,
            position = mutation.Position.ToString(),
            pluginPath = arguments.GetOptionalString("custom-mutation-plugin"),
            typeName = arguments.GetOptionalString("custom-mutation-type"),
            parameters = mutation.Parameters.Count == 0 ? null : mutation.Parameters
        };
    }

    private static object? CreateCustomChunkMutationConfigDocument(CliArguments arguments, CustomChunkMutationParameters? mutation)
    {
        if (mutation is null)
        {
            return null;
        }

        return new
        {
            name = mutation.Name,
            position = mutation.Position.ToString(),
            pluginPath = arguments.GetOptionalString("custom-chunk-mutation-plugin"),
            typeName = arguments.GetOptionalString("custom-chunk-mutation-type"),
            parameters = mutation.Parameters.Count == 0 ? null : mutation.Parameters
        };
    }

    private static void AppendForwardedArgument(List<string> args, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        args.Add($"--{key}");
        args.Add(QuoteArgument(value));
    }

    private static string QuoteArgument(string value)
    {
        return value.Contains(' ') ? $"\"{value.Replace("\"", "\\\"")}\"" : value;
    }

    private static void EnsureParentDirectory(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static NibbleSwapKind? ParseOptionalNibbleSwap(string? value, PermutationKind permutationKind)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return ParseNibbleSwapKind(value);
        }

        return permutationKind == PermutationKind.NibbleSwap ? NibbleSwapKind.ReverseNibbles : null;
    }

    private static ChunkPermutationVariant? ParseOptionalChunkPermutationVariant(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : ParseChunkPermutationVariant(value);
    }

    private static FeistelRoundFunctionKind? ParseOptionalFeistelRoundFunction(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : ParseFeistelRoundFunctionKind(value);
    }

    private static NumberKind ParseNumberKind(string value) => value.ToLowerInvariant() switch
    {
        "uint32" => NumberKind.UInt32,
        "uint64" => NumberKind.UInt64,
        _ => throw new CliUsageException($"Unsupported number kind '{value}'.")
    };

    private static BinaryKind ParseBinaryKind(string value) => value.ToLowerInvariant() switch
    {
        "fixed-unsigned" => BinaryKind.FixedUnsigned,
        _ => throw new CliUsageException($"Unsupported binary kind '{value}'.")
    };

    private static BitOrderKind ParseBitOrderKind(string value) => value.ToLowerInvariant() switch
    {
        "msb" or "msbfirst" => BitOrderKind.MsbFirst,
        "lsb" or "lsbfirst" => BitOrderKind.LsbFirst,
        _ => throw new CliUsageException($"Unsupported bit order '{value}'.")
    };

    private static ByteOrderKind ParseByteOrderKind(string value) => value.ToLowerInvariant() switch
    {
        "big" or "bigendian" => ByteOrderKind.BigEndian,
        "little" or "littleendian" => ByteOrderKind.LittleEndian,
        _ => throw new CliUsageException($"Unsupported byte order '{value}'.")
    };

    private static MixerKind ParseMixerKind(string value) => value.ToLowerInvariant() switch
    {
        "none" => MixerKind.None,
        "xor" => MixerKind.Xor,
        "add" => MixerKind.Add,
        "multiply" => MixerKind.Multiply,
        _ => throw new CliUsageException($"Unsupported mixer kind '{value}'.")
    };

    private static SaltDerivationKind ParseSaltDerivationKind(string value) => value.ToLowerInvariant() switch
    {
        "none" => SaltDerivationKind.None,
        "direct" or "usesaltseeddirectly" => SaltDerivationKind.UseSaltSeedDirectly,
        "splitmix64" => SaltDerivationKind.SplitMix64,
        _ => throw new CliUsageException($"Unsupported salt derivation '{value}'.")
    };

    private static PermutationKind ParsePermutationKind(string value) => value.ToLowerInvariant() switch
    {
        "identity" => PermutationKind.Identity,
        "not" => PermutationKind.Not,
        "rotate" => PermutationKind.Rotate,
        "byteswap" => PermutationKind.ByteSwap,
        "bitreverse" => PermutationKind.BitReverse,
        "nibbleswap" => PermutationKind.NibbleSwap,
        "chunk" or "chunkpermutation" => PermutationKind.ChunkPermutation,
        "feistel" => PermutationKind.Feistel,
        _ => throw new CliUsageException($"Unsupported permutation kind '{value}'.")
    };

    private static NibbleSwapKind ParseNibbleSwapKind(string value) => value.ToLowerInvariant() switch
    {
        "reverse" or "reversenibbles" => NibbleSwapKind.ReverseNibbles,
        "swap-adjacent" or "swapadjacentnibbles" => NibbleSwapKind.SwapAdjacentNibbles,
        _ => throw new CliUsageException($"Unsupported nibble swap variant '{value}'.")
    };

    private static ChunkPermutationVariant ParseChunkPermutationVariant(string value) => value.ToLowerInvariant() switch
    {
        "explicit" => ChunkPermutationVariant.ExplicitOrder,
        "reverse" => ChunkPermutationVariant.ReverseGroups,
        "rotate-left" => ChunkPermutationVariant.RotateGroupsLeft,
        "rotate-right" => ChunkPermutationVariant.RotateGroupsRight,
        "swap-adjacent" => ChunkPermutationVariant.SwapAdjacentGroups,
        "salt-shuffle" => ChunkPermutationVariant.SaltShuffle,
        _ => throw new CliUsageException($"Unsupported chunk permutation variant '{value}'.")
    };

    private static FeistelRoundFunctionKind ParseFeistelRoundFunctionKind(string value) => value.ToLowerInvariant() switch
    {
        "xorshift-add" => FeistelRoundFunctionKind.XorShiftAdd,
        "multiply-xor" => FeistelRoundFunctionKind.MultiplyXor,
        _ => throw new CliUsageException($"Unsupported Feistel round function '{value}'.")
    };

    private static ChunkerKind ParseChunkerKind(string value) => value.ToLowerInvariant() switch
    {
        "fixed" => ChunkerKind.Fixed,
        _ => throw new CliUsageException($"Unsupported chunker kind '{value}'.")
    };

    private static EmitterKind ParseEmitterKind(string value) => value.ToLowerInvariant() switch
    {
        "hex16" => EmitterKind.Hex16,
        "base32" or "base32crockford" => EmitterKind.Base32Crockford,
        "base64url" => EmitterKind.Base64Url,
        "bytes" or "bytearray" => EmitterKind.ByteArray,
        "custom" or "customalphabet" => EmitterKind.CustomAlphabet,
        _ => throw new CliUsageException($"Unsupported emitter kind '{value}'.")
    };

    private static AlphabetKind ParseAlphabetKind(string value) => value.ToLowerInvariant() switch
    {
        "none" => AlphabetKind.None,
        "hex16" => AlphabetKind.Hex16,
        "base32-crockford" or "base32crockford" => AlphabetKind.Base32Crockford,
        "base64url" => AlphabetKind.Base64Url,
        "custom" => AlphabetKind.Custom,
        _ => throw new CliUsageException($"Unsupported alphabet kind '{value}'.")
    };

    private static OutputKind ParseOutputKind(string value) => value.ToLowerInvariant() switch
    {
        "string" => OutputKind.String,
        "char-array" or "chararray" => OutputKind.CharArray,
        "byte-array" or "bytearray" => OutputKind.ByteArray,
        _ => throw new CliUsageException($"Unsupported output kind '{value}'.")
    };

    private static ByteArrayTextFormat ParseByteArrayTextFormat(string value) => value.ToLowerInvariant() switch
    {
        "hex" => ByteArrayTextFormat.Hex,
        "base64" => ByteArrayTextFormat.Base64,
        "csv-decimal" => ByteArrayTextFormat.CommaSeparatedDecimal,
        _ => throw new CliUsageException($"Unsupported byte-array format '{value}'.")
    };

    private static BenchmarkProfileKind ParseBenchmarkProfileKind(string value) => value.ToLowerInvariant() switch
    {
        "quick" => BenchmarkProfileKind.Quick,
        "default" => BenchmarkProfileKind.Default,
        "full" => BenchmarkProfileKind.Full,
        _ => throw new CliUsageException($"Unsupported benchmark profile '{value}'.")
    };

    private static WeightingProfileKind ParseWeightingProfileKind(string value) => value.ToLowerInvariant() switch
    {
        "smoke" => WeightingProfileKind.Smoke,
        "speed-first" => WeightingProfileKind.SpeedFirst,
        "balanced" => WeightingProfileKind.Balanced,
        "exploratory" => WeightingProfileKind.Exploratory,
        "exhaustive" => WeightingProfileKind.Exhaustive,
        _ => throw new CliUsageException($"Unsupported weighting profile '{value}'.")
    };

    private static BenchmarkModeKind ParseBenchmarkModeKind(string value) => value.ToLowerInvariant() switch
    {
        "quick" => BenchmarkModeKind.Quick,
        "benchmarkdotnet" => BenchmarkModeKind.BenchmarkDotNet,
        _ => throw new CliUsageException($"Unsupported benchmark mode '{value}'.")
    };

    private static BenchmarkValueSetKind ParseBenchmarkValueSetKind(string value) => value.ToLowerInvariant() switch
    {
        "default" => BenchmarkValueSetKind.Default,
        "small" => BenchmarkValueSetKind.Small,
        "middle" => BenchmarkValueSetKind.Middle,
        "large" => BenchmarkValueSetKind.Large,
        "max" => BenchmarkValueSetKind.Max,
        _ => throw new CliUsageException($"Unsupported benchmark value set '{value}'.")
    };

    private static CustomMutationPosition ParseCustomMutationPosition(string value) => value.ToLowerInvariant() switch
    {
        "before-mix" => CustomMutationPosition.BeforeMix,
        "after-mix" => CustomMutationPosition.AfterMix,
        "after-permutation" => CustomMutationPosition.AfterPermutation,
        _ => throw new CliUsageException($"Unsupported custom mutation position '{value}'.")
    };

    private static CustomChunkMutationPosition ParseCustomChunkMutationPosition(string value) => value.ToLowerInvariant() switch
    {
        "before-emit" => CustomChunkMutationPosition.BeforeEmit,
        _ => throw new CliUsageException($"Unsupported custom chunk mutation position '{value}'.")
    };

    private static AlphabetKind DefaultAlphabetKind(EmitterKind kind) => kind switch
    {
        EmitterKind.Hex16 => AlphabetKind.Hex16,
        EmitterKind.Base32Crockford => AlphabetKind.Base32Crockford,
        EmitterKind.Base64Url => AlphabetKind.Base64Url,
        EmitterKind.ByteArray => AlphabetKind.None,
        EmitterKind.CustomAlphabet => AlphabetKind.Custom,
        _ => throw new CliUsageException($"No default alphabet for emitter kind '{kind}'.")
    };

    private static OutputKind DefaultOutputKind(EmitterKind kind) => kind switch
    {
        EmitterKind.ByteArray => OutputKind.ByteArray,
        _ => OutputKind.String
    };

    private static WeightingProfileKind DefaultWeightingProfile(BenchmarkProfileKind kind) => kind switch
    {
        BenchmarkProfileKind.Quick => WeightingProfileKind.SpeedFirst,
        BenchmarkProfileKind.Default => WeightingProfileKind.Balanced,
        BenchmarkProfileKind.Full => WeightingProfileKind.Exhaustive,
        _ => throw new CliUsageException($"No default weighting profile for benchmark profile '{kind}'.")
    };

    private static ulong ParseULongInvariant(string value, string flagName)
    {
        if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong parsed))
        {
            return parsed;
        }

        throw new CliUsageException($"Flag '--{flagName}' requires an unsigned integer value.");
    }

    private static int Fail(TextWriter stderr, string message)
    {
        stderr.WriteLine(message);
        return 1;
    }

    private static void PrintUsage(TextWriter stderr)
    {
        stderr.WriteLine("Usage:");
        stderr.WriteLine("  bpl encode --value <number> --bits <n> --chunk-size <n> --mix <kind> --permute <kind> --emitter <kind> [common flags]");
        stderr.WriteLine("  bpl decode --value <text-or-byte-array> --bits <n> --chunk-size <n> --mix <kind> --permute <kind> --emitter <kind> [common flags]");
        stderr.WriteLine("  bpl list");
        stderr.WriteLine("  bpl benchmark --profile quick|default|full [--iterations <n>] [--weighting-profile <kind>] [--scenario-budget <n>] [--sampling-seed <ulong>]");
        stderr.WriteLine("  bpl benchmark --config <benchmark.json> [--output <report.md>] [--csv <report.csv>]");
        stderr.WriteLine("Common flags:");
        stderr.WriteLine("  --scenario-name <name>");
        stderr.WriteLine("  --number-kind uint32|uint64");
        stderr.WriteLine("  --bits <8|16|24|32|40|48|56|64>");
        stderr.WriteLine("  --salt <ulong> | --salt-text <text>");
        stderr.WriteLine("  --bin fixed-unsigned");
        stderr.WriteLine("  --bit-order msb|lsb");
        stderr.WriteLine("  --byte-order big|little");
        stderr.WriteLine("  --mix none|xor|add|multiply");
        stderr.WriteLine("  --mask-derivation none|direct|splitmix64");
        stderr.WriteLine("  --xor-mask <ulong> | --addend <ulong> | --multiplier <odd ulong>");
        stderr.WriteLine("  --permute identity|not|rotate|byteswap|bitreverse|nibbleswap|chunk|feistel");
        stderr.WriteLine("  --rotate-by <int>");
        stderr.WriteLine("  --nibble-swap reverse|swap-adjacent");
        stderr.WriteLine("  --chunk-permute-group-size <int> --chunk-permute-variant <kind> [--chunk-permute-order <csv>] [--chunk-permute-rotate-by <int>]");
        stderr.WriteLine("  --feistel-rounds <1|2|3|4> --feistel-round-function xorshift-add|multiply-xor");
        stderr.WriteLine("  --chunker fixed --chunk-size <4|5|6|8> --chunk-read-order msb|lsb");
        stderr.WriteLine("  --emitter hex16|base32|base64url|bytes|custom");
        stderr.WriteLine("  --alphabet none|hex16|base32-crockford|base64url|custom");
        stderr.WriteLine("  --custom-alphabet <string>");
        stderr.WriteLine("  --output-kind string|char-array|byte-array");
        stderr.WriteLine("  --byte-array-format hex|base64|csv-decimal");
        stderr.WriteLine("  --custom-mutation-name <name> --custom-mutation-position before-mix|after-mix|after-permutation --custom-mutation-param <key=value>");
        stderr.WriteLine("  --custom-mutation-plugin <assembly.dll> --custom-mutation-type <full.type.Name>");
        stderr.WriteLine("  --custom-chunk-mutation-name <name> --custom-chunk-mutation-position before-emit --custom-chunk-mutation-param <key=value>");
        stderr.WriteLine("  --custom-chunk-mutation-plugin <assembly.dll> --custom-chunk-mutation-type <full.type.Name>");
        stderr.WriteLine("Notes:");
        stderr.WriteLine("  Benchmark weighting profiles: smoke, speed-first, balanced, exploratory, exhaustive.");
        stderr.WriteLine("  --weights-config <path> can override built-in mixer, permutation, and emitter weights.");
        stderr.WriteLine("  Benchmark value sets: default, small, middle, large, max.");
        stderr.WriteLine("  Plugin-loaded custom mutations must come from a public parameterless type implementing ICustomMutation or ICustomChunkMutation.");
    }

    private static string FormatNames(IReadOnlyCollection<string> names)
    {
        return names.Count == 0 ? "(none)" : string.Join(", ", names.OrderBy(static name => name, StringComparer.Ordinal));
    }
}

internal sealed class CliArguments
{
    private readonly Dictionary<string, List<string>> _values;

    private CliArguments(Dictionary<string, List<string>> values)
    {
        _values = values;
    }

    public static CliArguments Parse(IEnumerable<string> args)
    {
        Dictionary<string, List<string>> values = new(StringComparer.OrdinalIgnoreCase);
        string? pendingKey = null;

        foreach (string arg in args)
        {
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                if (pendingKey is not null)
                {
                    throw new CliUsageException($"Missing value for '--{pendingKey}'.");
                }

                pendingKey = arg[2..];
                continue;
            }

            if (pendingKey is null)
            {
                throw new CliUsageException($"Unexpected argument '{arg}'.");
            }

            if (!values.TryGetValue(pendingKey, out List<string>? list))
            {
                list = [];
                values[pendingKey] = list;
            }

            list.Add(arg);
            pendingKey = null;
        }

        if (pendingKey is not null)
        {
            throw new CliUsageException($"Missing value for '--{pendingKey}'.");
        }

        return new CliArguments(values);
    }

    public bool Contains(string key) => _values.ContainsKey(key);

    public bool TryGetValue(string key, out string? value)
    {
        if (_values.TryGetValue(key, out List<string>? values) && values.Count > 0)
        {
            value = values[^1];
            return true;
        }

        value = null;
        return false;
    }

    public string GetRequiredString(string key)
    {
        return TryGetValue(key, out string? value)
            ? value!
            : throw new CliUsageException($"Missing required flag '--{key}'.");
    }

    public string? GetOptionalString(string key) => TryGetValue(key, out string? value) ? value : null;

    public IReadOnlyList<string> GetAllStrings(string key)
    {
        return _values.TryGetValue(key, out List<string>? values) ? values : Array.Empty<string>();
    }

    public int GetRequiredInt(string key)
    {
        string value = GetRequiredString(key);
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            return parsed;
        }

        throw new CliUsageException($"Flag '--{key}' requires an integer value.");
    }

    public ulong GetRequiredULong(string key)
    {
        string value = GetRequiredString(key);
        if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong parsed))
        {
            return parsed;
        }

        throw new CliUsageException($"Flag '--{key}' requires an unsigned integer value.");
    }

    public int? GetOptionalInt(string key)
    {
        if (!TryGetValue(key, out string? value))
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            return parsed;
        }

        throw new CliUsageException($"Flag '--{key}' requires an integer value.");
    }

    public ulong? GetOptionalULong(string key)
    {
        if (!TryGetValue(key, out string? value))
        {
            return null;
        }

        if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong parsed))
        {
            return parsed;
        }

        throw new CliUsageException($"Flag '--{key}' requires an unsigned integer value.");
    }

    public bool? GetOptionalBool(string key)
    {
        if (!TryGetValue(key, out string? value))
        {
            return null;
        }

        return value!.ToLowerInvariant() switch
        {
            "true" => true,
            "false" => false,
            _ => throw new CliUsageException($"Flag '--{key}' requires true or false.")
        };
    }

    public TEnum GetEnum<TEnum>(string key, TEnum defaultValue, Func<string, TEnum> parser)
    {
        return TryGetValue(key, out string? value) ? parser(value!) : defaultValue;
    }
}

internal sealed class CliUsageException : Exception
{
    public CliUsageException(string message) : base(message)
    {
    }
}
