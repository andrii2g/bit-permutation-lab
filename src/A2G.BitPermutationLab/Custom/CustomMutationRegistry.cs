using A2G.BitPermutationLab.Abstractions;

namespace A2G.BitPermutationLab.Custom;

public static class CustomMutationRegistry
{
    private static readonly Dictionary<string, ICustomMutation> Mutations = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, ICustomChunkMutation> ChunkMutations = new(StringComparer.Ordinal);

    public static void Register(ICustomMutation mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        Register(mutation.Name, mutation);
    }

    public static void Register(ICustomChunkMutation mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        Register(mutation.Name, mutation);
    }

    public static void Register(string name, ICustomMutation mutation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(mutation);
        Mutations[name] = mutation;
    }

    public static void Register(string name, ICustomChunkMutation mutation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(mutation);
        ChunkMutations[name] = mutation;
    }

    public static bool TryGetMutation(string name, out ICustomMutation mutation) =>
        Mutations.TryGetValue(name, out mutation!);

    public static bool TryGetChunkMutation(string name, out ICustomChunkMutation mutation) =>
        ChunkMutations.TryGetValue(name, out mutation!);

    public static IReadOnlyCollection<string> GetRegisteredMutationNames() => Mutations.Keys.ToArray();

    public static IReadOnlyCollection<string> GetRegisteredChunkMutationNames() => ChunkMutations.Keys.ToArray();
}
