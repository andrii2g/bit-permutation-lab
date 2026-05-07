using A2G.BitPermutationLab.Benchmarking;

namespace A2G.BitPermutationLab.Benchmarks;

internal static class Program
{
    private static int Main(string[] args)
    {
        BenchmarkCommandLine options = BenchmarkCommandLine.Parse(args);
        BenchmarkExecutionOptions execution = new(
            options.Profile.ToString(),
            options.Iterations,
            new BenchmarkSelectionOptions(options.WeightingProfile, options.ScenarioBudget, options.SamplingSeed, options.IncludeRequiredBaselines),
            new BenchmarkReportOptions(options.ReportWeighted, options.ReportUnweighted));

        IReadOnlyList<BenchmarkResultRow> rows = BenchmarkRunner.Run(execution);

        Console.WriteLine($"Benchmark profile: {options.Profile}");
        Console.WriteLine($"Weighting profile: {options.WeightingProfile}");
        Console.WriteLine($"Iterations per value: {options.Iterations}");
        if (options.ScenarioBudget is not null)
        {
            Console.WriteLine($"Scenario budget: {options.ScenarioBudget}");
        }

        Console.WriteLine($"Sampling seed: {options.SamplingSeed}");
        Console.WriteLine($"Include required baselines: {options.IncludeRequiredBaselines}");
        BenchmarkConsoleFormatter.Write(rows, Console.Out, execution.Report);
        return 0;
    }
}

internal sealed record BenchmarkCommandLine(
    BenchmarkProfileKind Profile,
    WeightingProfileKind WeightingProfile,
    int Iterations,
    int? ScenarioBudget,
    ulong SamplingSeed,
    bool IncludeRequiredBaselines,
    bool ReportWeighted,
    bool ReportUnweighted)
{
    public static BenchmarkCommandLine Parse(string[] args)
    {
        BenchmarkProfileKind profile = BenchmarkProfileKind.Default;
        WeightingProfileKind weightingProfile = WeightingProfileKind.Balanced;
        int iterations = 10_000;
        int? scenarioBudget = BenchmarkProfileFactory.CreateSelectionOptions(profile).ScenarioBudget;
        ulong samplingSeed = BenchmarkProfileFactory.CreateSelectionOptions(profile).SamplingSeed;
        bool includeRequiredBaselines = true;
        bool reportWeighted = true;
        bool reportUnweighted = true;

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
                default:
                    throw new ArgumentException($"Unsupported benchmark argument '--{key}'.");
            }
        }

        if (!reportWeighted && !reportUnweighted)
        {
            throw new ArgumentException("At least one report mode must be enabled.");
        }

        if (iterations <= 0)
        {
            throw new ArgumentException("Iterations must be greater than zero.");
        }

        return new BenchmarkCommandLine(profile, weightingProfile, iterations, scenarioBudget, samplingSeed, includeRequiredBaselines, reportWeighted, reportUnweighted);
    }

    private static BenchmarkProfileKind ParseProfile(string value) => value.ToLowerInvariant() switch
    {
        "quick" => BenchmarkProfileKind.Quick,
        "default" => BenchmarkProfileKind.Default,
        "full" => BenchmarkProfileKind.Full,
        _ => throw new ArgumentException($"Unsupported benchmark profile '{value}'.")
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
