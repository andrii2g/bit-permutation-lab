namespace A2G.BitPermutationLab.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        return CliApplication.Run(args, Console.Out, Console.Error);
    }
}
