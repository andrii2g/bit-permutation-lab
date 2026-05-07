using System.Reflection;
using A2G.BitPermutationLab.Abstractions;

namespace A2G.BitPermutationLab.Custom;

public static class CustomMutationPluginLoader
{
    public static string LoadBitMutation(string assemblyPath, string typeName, string? registrationName = null)
    {
        ICustomMutation mutation = CreateInstance<ICustomMutation>(assemblyPath, typeName);
        string resolvedName = registrationName ?? mutation.Name;
        CustomMutationRegistry.Register(resolvedName, mutation);
        return resolvedName;
    }

    public static string LoadChunkMutation(string assemblyPath, string typeName, string? registrationName = null)
    {
        ICustomChunkMutation mutation = CreateInstance<ICustomChunkMutation>(assemblyPath, typeName);
        string resolvedName = registrationName ?? mutation.Name;
        CustomMutationRegistry.Register(resolvedName, mutation);
        return resolvedName;
    }

    private static TInterface CreateInstance<TInterface>(string assemblyPath, string typeName)
        where TInterface : class
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            throw new InvalidOperationException("Custom mutation plugin assembly path is required.");
        }

        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new InvalidOperationException("Custom mutation plugin type name is required.");
        }

        string fullAssemblyPath = Path.GetFullPath(assemblyPath);
        if (!File.Exists(fullAssemblyPath))
        {
            throw new FileNotFoundException($"Custom mutation plugin assembly was not found at '{fullAssemblyPath}'.", fullAssemblyPath);
        }

        Assembly assembly = Assembly.LoadFrom(fullAssemblyPath);
        Type pluginType = assembly.GetType(typeName, throwOnError: false, ignoreCase: false)
            ?? throw new InvalidOperationException($"Type '{typeName}' was not found in '{fullAssemblyPath}'.");

        if (!typeof(TInterface).IsAssignableFrom(pluginType))
        {
            throw new InvalidOperationException(
                $"Type '{typeName}' from '{fullAssemblyPath}' does not implement {typeof(TInterface).Name}.");
        }

        if (Activator.CreateInstance(pluginType) is not TInterface instance)
        {
            throw new InvalidOperationException(
                $"Type '{typeName}' from '{fullAssemblyPath}' could not be constructed. A public parameterless constructor is required.");
        }

        return instance;
    }
}
