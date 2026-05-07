using A2G.BitPermutationLab.Benchmarking;

namespace A2G.BitPermutationLab.Benchmarks;

internal static class Program
{
    private static int Main(string[] args)
    {
        BenchmarkProfileKind profile = ParseProfile(args);
        IReadOnlyList<BenchmarkResultRow> rows = BenchmarkRunner.Run(profile, 10_000);

        Console.WriteLine($"Benchmark profile: {profile}");
        Console.WriteLine("Iterations per value: 10000");
        BenchmarkConsoleFormatter.Write(rows, Console.Out);
        return 0;
    }

    private static BenchmarkProfileKind ParseProfile(string[] args)
    {
        if (args.Length == 0)
        {
            return BenchmarkProfileKind.Default;
        }

        return args[0].ToLowerInvariant() switch
        {
            "quick" => BenchmarkProfileKind.Quick,
            "default" => BenchmarkProfileKind.Default,
            "full" => BenchmarkProfileKind.Full,
            _ => BenchmarkProfileKind.Default
        };
    }
}
