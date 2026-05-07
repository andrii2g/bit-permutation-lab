using A2G.BitPermutationLab.Benchmarking;

namespace A2G.BitPermutationLab.Benchmarks;

internal static class Program
{
    private static int Main(string[] args)
    {
        BenchmarkCommandLine options = BenchmarkCommandLine.Parse(args);

        BenchmarkRunResult result;
        if (!string.IsNullOrWhiteSpace(options.ConfigPath))
        {
            LoadedBenchmarkConfig loaded = BenchmarkConfigLoader.Load(options.ConfigPath);
            BenchmarkExecutionOptions execution = loaded.Options with
            {
                Iterations = options.Iterations ?? loaded.Options.Iterations,
                Report = new BenchmarkReportOptions(options.ReportWeighted, options.ReportUnweighted),
                ValidateScenarios = options.Validate ?? loaded.Options.ValidateScenarios
            };

            result = BenchmarkRunner.RunDetailed(loaded.Scenarios, execution);
            Console.WriteLine($"Benchmark profile: {execution.ProfileLabel}");
            Console.WriteLine($"Benchmark mode: {execution.ModeLabel}");
            Console.WriteLine($"Iterations per value: {execution.Iterations}");
            BenchmarkConsoleFormatter.Write(result.Rows, Console.Out, execution.Report);

            if (!string.IsNullOrWhiteSpace(options.OutputMarkdown ?? loaded.OutputMarkdown))
            {
                WriteMarkdown(result, options.OutputMarkdown ?? loaded.OutputMarkdown!, loaded.Top);
            }

            if (!string.IsNullOrWhiteSpace(options.OutputCsv ?? loaded.OutputCsv))
            {
                WriteCsv(result, options.OutputCsv ?? loaded.OutputCsv!);
            }

            return 0;
        }

        BenchmarkExecutionOptions directExecution = new(
            options.Profile.ToString(),
            options.Mode.ToString(),
            options.Iterations ?? 10_000,
            new BenchmarkSelectionOptions(options.WeightingProfile, options.ScenarioBudget, options.SamplingSeed, options.IncludeRequiredBaselines),
            new BenchmarkReportOptions(options.ReportWeighted, options.ReportUnweighted),
            options.Validate ?? true);

        result = BenchmarkRunner.RunDetailed(directExecution);
        Console.WriteLine($"Benchmark profile: {options.Profile}");
        Console.WriteLine($"Benchmark mode: {options.Mode}");
        Console.WriteLine($"Weighting profile: {options.WeightingProfile}");
        Console.WriteLine($"Iterations per value: {directExecution.Iterations}");
        if (options.ScenarioBudget is not null)
        {
            Console.WriteLine($"Scenario budget: {options.ScenarioBudget}");
        }

        Console.WriteLine($"Sampling seed: {options.SamplingSeed}");
        Console.WriteLine($"Include required baselines: {options.IncludeRequiredBaselines}");
        BenchmarkConsoleFormatter.Write(result.Rows, Console.Out, directExecution.Report);
        if (!string.IsNullOrWhiteSpace(options.OutputMarkdown))
        {
            WriteMarkdown(result, options.OutputMarkdown, options.Top);
        }

        if (!string.IsNullOrWhiteSpace(options.OutputCsv))
        {
            WriteCsv(result, options.OutputCsv);
        }

        return 0;
    }

    private static void WriteMarkdown(BenchmarkRunResult result, string path, int top)
    {
        EnsureParentDirectory(path);
        using StreamWriter writer = File.CreateText(path);
        MarkdownBenchmarkReportWriter.Write(result, writer, top);
        Console.WriteLine($"Markdown report: {path}");
    }

    private static void WriteCsv(BenchmarkRunResult result, string path)
    {
        EnsureParentDirectory(path);
        using StreamWriter writer = File.CreateText(path);
        CsvBenchmarkReportWriter.Write(result, writer);
        Console.WriteLine($"CSV report: {path}");
    }

    private static void EnsureParentDirectory(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}

internal sealed record BenchmarkCommandLine(
    BenchmarkProfileKind Profile,
    BenchmarkModeKind Mode,
    WeightingProfileKind WeightingProfile,
    int? Iterations,
    int? ScenarioBudget,
    ulong SamplingSeed,
    bool IncludeRequiredBaselines,
    bool ReportWeighted,
    bool ReportUnweighted,
    bool? Validate,
    int Top,
    string? OutputMarkdown,
    string? OutputCsv,
    string? ConfigPath)
{
    public static BenchmarkCommandLine Parse(string[] args)
    {
        BenchmarkProfileKind profile = BenchmarkProfileKind.Default;
        BenchmarkModeKind mode = BenchmarkModeKind.Quick;
        WeightingProfileKind weightingProfile = WeightingProfileKind.Balanced;
        int? iterations = null;
        int? scenarioBudget = BenchmarkProfileFactory.CreateSelectionOptions(profile).ScenarioBudget;
        ulong samplingSeed = BenchmarkProfileFactory.CreateSelectionOptions(profile).SamplingSeed;
        bool includeRequiredBaselines = true;
        bool reportWeighted = true;
        bool reportUnweighted = true;
        bool? validate = null;
        int top = 5;
        string? outputMarkdown = null;
        string? outputCsv = null;
        string? configPath = null;

        int index = 0;
        if (args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal))
        {
            profile = ParseProfile(args[0]);
            weightingProfile = DefaultWeightingProfile(profile);
            scenarioBudget = BenchmarkProfileFactory.CreateSelectionOptions(profile).ScenarioBudget;
            samplingSeed = BenchmarkProfileFactory.CreateSelectionOptions(profile).SamplingSeed;
            index = 1;
        }

        while (index < args.Length)
        {
            string arg = args[index];
            if (!arg.StartsWith("--", StringComparison.Ordinal) || index == args.Length - 1)
            {
                throw new ArgumentException($"Unexpected benchmark argument '{arg}'.");
            }

            string key = arg[2..];
            string value = args[index + 1];
            index += 2;

            switch (key.ToLowerInvariant())
            {
                case "profile":
                    profile = ParseProfile(value);
                    weightingProfile = DefaultWeightingProfile(profile);
                    scenarioBudget = BenchmarkProfileFactory.CreateSelectionOptions(profile).ScenarioBudget;
                    samplingSeed = BenchmarkProfileFactory.CreateSelectionOptions(profile).SamplingSeed;
                    break;
                case "mode":
                    mode = ParseMode(value);
                    break;
                case "weighting-profile":
                    weightingProfile = ParseWeightingProfile(value);
                    break;
                case "iterations":
                    iterations = int.Parse(value);
                    break;
                case "scenario-budget":
                    scenarioBudget = int.Parse(value);
                    break;
                case "sampling-seed":
                    samplingSeed = ulong.Parse(value);
                    break;
                case "include-required-baselines":
                    includeRequiredBaselines = bool.Parse(value);
                    break;
                case "report-weighted":
                    reportWeighted = bool.Parse(value);
                    break;
                case "report-unweighted":
                    reportUnweighted = bool.Parse(value);
                    break;
                case "validate":
                    validate = bool.Parse(value);
                    break;
                case "top":
                    top = int.Parse(value);
                    break;
                case "output":
                    outputMarkdown = value;
                    break;
                case "csv":
                    outputCsv = value;
                    break;
                case "config":
                    configPath = value;
                    break;
                default:
                    throw new ArgumentException($"Unsupported benchmark argument '--{key}'.");
            }
        }

        if (!reportWeighted && !reportUnweighted)
        {
            throw new ArgumentException("At least one report mode must be enabled.");
        }

        if (iterations is <= 0)
        {
            throw new ArgumentException("Iterations must be greater than zero.");
        }

        return new BenchmarkCommandLine(profile, mode, weightingProfile, iterations, scenarioBudget, samplingSeed, includeRequiredBaselines, reportWeighted, reportUnweighted, validate, top, outputMarkdown, outputCsv, configPath);
    }

    private static BenchmarkProfileKind ParseProfile(string value) => value.ToLowerInvariant() switch
    {
        "quick" => BenchmarkProfileKind.Quick,
        "default" => BenchmarkProfileKind.Default,
        "full" => BenchmarkProfileKind.Full,
        _ => throw new ArgumentException($"Unsupported benchmark profile '{value}'.")
    };

    private static BenchmarkModeKind ParseMode(string value) => value.ToLowerInvariant() switch
    {
        "quick" => BenchmarkModeKind.Quick,
        "benchmarkdotnet" => BenchmarkModeKind.BenchmarkDotNet,
        _ => throw new ArgumentException($"Unsupported benchmark mode '{value}'.")
    };

    private static WeightingProfileKind ParseWeightingProfile(string value) => value.ToLowerInvariant() switch
    {
        "smoke" => WeightingProfileKind.Smoke,
        "speed-first" => WeightingProfileKind.SpeedFirst,
        "balanced" => WeightingProfileKind.Balanced,
        "exploratory" => WeightingProfileKind.Exploratory,
        "exhaustive" => WeightingProfileKind.Exhaustive,
        _ => throw new ArgumentException($"Unsupported weighting profile '{value}'.")
    };

    private static WeightingProfileKind DefaultWeightingProfile(BenchmarkProfileKind profile) => profile switch
    {
        BenchmarkProfileKind.Quick => WeightingProfileKind.SpeedFirst,
        BenchmarkProfileKind.Default => WeightingProfileKind.Balanced,
        BenchmarkProfileKind.Full => WeightingProfileKind.Exhaustive,
        _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unsupported benchmark profile.")
    };
}
