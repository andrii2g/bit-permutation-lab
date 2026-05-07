using A2G.BitPermutationLab.Benchmarking;
using A2G.BitPermutationLab.Core;
using A2G.BitPermutationLab.Custom;

namespace A2G.BitPermutationLab.Tests;

public sealed class BenchmarkConfigLoaderTests
{
    [Fact]
    public void Load_ParsesScenarioAndResolvesSaltText()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path,
                """
                {
                  "scenarios": [
                    {
                      "name": "xor-rotate-hex32",
                      "values": [1, 2, 1000],
                      "parameters": {
                        "name": "xor-rotate-hex32",
                        "numberKind": "UInt32",
                        "bitLength": 32,
                        "saltText": "HelloSalt",
                        "binary": { "kind": "FixedUnsigned", "bitOrder": "MsbFirst", "byteOrder": "BigEndian" },
                        "mixer": { "kind": "Xor", "maskDerivation": "SplitMix64" },
                        "permutation": { "kind": "Rotate", "rotateBy": 11 },
                        "chunking": { "kind": "Fixed", "chunkSize": 4, "chunkReadOrder": "MsbFirst" },
                        "emitter": { "kind": "Hex16", "alphabetKind": "Hex16", "outputKind": "String", "byteArrayTextFormat": "Hex" }
                      }
                    }
                  ],
                  "benchmark": {
                    "mode": "Quick",
                    "iterations": 123,
                    "validate": true,
                    "top": 7
                  }
                }
                """);

            LoadedBenchmarkConfig loaded = BenchmarkConfigLoader.Load(path);

            Assert.Single(loaded.Scenarios);
            Assert.Equal(123, loaded.Options.Iterations);
            Assert.Equal("Quick", loaded.Options.ModeLabel);
            Assert.Equal(7, loaded.Top);
            Assert.Equal(SaltDerivation.DeriveSaltSeedFromText("HelloSalt"), loaded.Scenarios[0].Parameters.SaltSeed);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_CanRegisterPluginBackedCustomMutation()
    {
        string path = Path.GetTempFileName();
        string pluginPath = typeof(PluginXorMutation).Assembly.Location.Replace("\\", "\\\\");
        string pluginType = typeof(PluginXorMutation).FullName!;

        try
        {
            File.WriteAllText(path,
                $$"""
                {
                  "scenarios": [
                    {
                      "name": "plugin-xor-rotate-hex32",
                      "values": [1, 2],
                      "parameters": {
                        "name": "plugin-xor-rotate-hex32",
                        "numberKind": "UInt32",
                        "bitLength": 32,
                        "saltSeed": 42,
                        "binary": { "kind": "FixedUnsigned", "bitOrder": "MsbFirst", "byteOrder": "BigEndian" },
                        "mixer": { "kind": "Xor", "maskDerivation": "SplitMix64" },
                        "permutation": { "kind": "Rotate", "rotateBy": 11 },
                        "chunking": { "kind": "Fixed", "chunkSize": 4, "chunkReadOrder": "MsbFirst" },
                        "emitter": { "kind": "Hex16", "alphabetKind": "Hex16", "outputKind": "String", "byteArrayTextFormat": "Hex" },
                        "customMutation": {
                          "name": "plugin-xor-config",
                          "position": "AfterMix",
                          "pluginPath": "{{pluginPath}}",
                          "typeName": "{{pluginType}}",
                          "parameters": { "source": "config" }
                        }
                      }
                    }
                  ]
                }
                """);

            LoadedBenchmarkConfig loaded = BenchmarkConfigLoader.Load(path);

            Assert.Single(loaded.Scenarios);
            Assert.Equal("plugin-xor-config", loaded.Scenarios[0].Parameters.CustomMutation?.Name);
            Assert.True(CustomMutationRegistry.TryGetMutation("plugin-xor-config", out _));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
