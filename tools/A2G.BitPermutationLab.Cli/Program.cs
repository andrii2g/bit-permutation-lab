using System.Globalization;
using A2G.BitPermutationLab.Binary;
using A2G.BitPermutationLab.Chunking;
using A2G.BitPermutationLab.Core;
using A2G.BitPermutationLab.Emitters;
using A2G.BitPermutationLab.Mixers;
using A2G.BitPermutationLab.Permutations;

namespace A2G.BitPermutationLab.Cli;

internal static class Program
{
    private static readonly CodecPipeline Pipeline = new();

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 1;
            }

            string command = args[0].ToLowerInvariant();
            CliArguments arguments = CliArguments.Parse(args[1..]);

            return command switch
            {
                "encode" => RunEncode(arguments),
                "decode" => RunDecode(arguments),
                "list" => NotImplemented("The 'list' command is not implemented in this iteration."),
                "benchmark" => NotImplemented("The 'benchmark' command is not implemented in this iteration."),
                _ => Fail($"Unknown command '{command}'.")
            };
        }
        catch (CliUsageException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int RunEncode(CliArguments arguments)
    {
        RejectUnsupportedCustomMutationFlags(arguments);

        ulong value = arguments.GetRequiredULong("value");
        CodecParameters parameters = CreateParameters(arguments);
        CodecResult result = Pipeline.Encode(value, parameters);

        switch (result.OutputKind)
        {
            case OutputKind.String:
                Console.WriteLine($"Encoded: {result.StringValue}");
                return 0;
            case OutputKind.CharArray:
                Console.WriteLine($"Encoded: {new string(result.CharArrayValue ?? Array.Empty<char>())}");
                return 0;
            case OutputKind.ByteArray:
                Console.WriteLine($"EncodedBytes{parameters.Emitter.ByteArrayTextFormat}: {FormatByteArray(result.ByteArrayValue ?? Array.Empty<byte>(), parameters.Emitter.ByteArrayTextFormat)}");
                return 0;
            default:
                return Fail($"Unsupported output kind '{result.OutputKind}'.");
        }
    }

    private static int RunDecode(CliArguments arguments)
    {
        RejectUnsupportedCustomMutationFlags(arguments);

        CodecParameters parameters = CreateParameters(arguments);
        DecodeResult result = parameters.Emitter.OutputKind == OutputKind.ByteArray
            ? Pipeline.Decode(ParseByteArrayInput(arguments.GetRequiredString("value"), parameters.Emitter.ByteArrayTextFormat), parameters)
            : Pipeline.Decode(arguments.GetRequiredString("value").AsSpan(), parameters);

        if (!result.Success)
        {
            return Fail(result.ErrorMessage ?? "Decode failed.");
        }

        Console.WriteLine($"Decoded: {result.Value}");
        return 0;
    }

    private static CodecParameters CreateParameters(CliArguments arguments)
    {
        ulong saltSeed = ResolveSaltSeed(arguments);
        RejectUnsupportedAdvancedFlags(arguments);

        NumberKind numberKind = arguments.GetEnum("number-kind", NumberKind.UInt64, ParseNumberKind);
        int bitLength = arguments.GetRequiredInt("bits");

        BinaryParameters binary = new(
            BinaryKind.FixedUnsigned,
            BitOrderKind.MsbFirst,
            ByteOrderKind.BigEndian);

        MixerKind mixerKind = arguments.GetEnum("mix", MixerKind.None, ParseMixerKind);
        MixerParameters mixer = new(
            mixerKind,
            SaltDerivationKind.SplitMix64);

        PermutationKind permutationKind = arguments.GetEnum("permute", PermutationKind.Identity, ParsePermutationKind);
        PermutationParameters permutation = new(
            permutationKind,
            RotateBy: permutationKind == PermutationKind.Rotate ? 11 : null,
            NibbleSwap: permutationKind == PermutationKind.NibbleSwap ? NibbleSwapKind.ReverseNibbles : null);

        ChunkingParameters chunking = new(
            ChunkerKind.Fixed,
            arguments.GetRequiredInt("chunk-size"),
            BitOrderKind.MsbFirst);

        EmitterKind emitterKind = arguments.GetEnum("emitter", EmitterKind.Hex16, ParseEmitterKind);
        EmitterParameters emitter = new(
            emitterKind,
            arguments.GetEnum("alphabet", DefaultAlphabetKind(emitterKind), ParseAlphabetKind),
            arguments.GetEnum("output-kind", DefaultOutputKind(emitterKind), ParseOutputKind),
            ByteArrayTextFormat: arguments.GetEnum("byte-array-format", ByteArrayTextFormat.Hex, ParseByteArrayTextFormat));

        return new CodecParameters(
            arguments.GetOptionalString("scenario-name") ?? "cli-scenario",
            numberKind,
            bitLength,
            saltSeed,
            binary,
            mixer,
            permutation,
            chunking,
            emitter);
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

    private static void RejectUnsupportedCustomMutationFlags(CliArguments arguments)
    {
        string[] unsupportedFlags =
        [
            "custom-mutation-name",
            "custom-mutation-position",
            "custom-mutation-param",
            "custom-mutation-plugin",
            "custom-mutation-type",
            "custom-chunk-mutation-name",
            "custom-chunk-mutation-position",
            "custom-chunk-mutation-param",
            "custom-chunk-mutation-plugin",
            "custom-chunk-mutation-type"
        ];

        foreach (string flag in unsupportedFlags)
        {
            if (arguments.Contains(flag))
            {
                throw new CliUsageException($"Flag '--{flag}' is not implemented in this CLI iteration.");
            }
        }
    }

    private static void RejectUnsupportedAdvancedFlags(CliArguments arguments)
    {
        string[] unsupportedFlags =
        [
            "bit-order",
            "byte-order",
            "mask-derivation",
            "xor-mask",
            "addend",
            "multiplier",
            "rotate-by",
            "nibble-swap",
            "chunk-permute-group-size",
            "chunk-permute-variant",
            "chunk-permute-order",
            "chunk-permute-rotate-by",
            "feistel-rounds",
            "feistel-round-function",
            "chunk-read-order",
            "custom-alphabet",
            "char-array"
        ];

        foreach (string flag in unsupportedFlags)
        {
            if (arguments.Contains(flag))
            {
                throw new CliUsageException($"Flag '--{flag}' is intentionally not exposed by the simplified CLI. Use code/library scenarios for advanced parameter control.");
            }
        }

        PermutationKind permutationKind = arguments.GetEnum("permute", PermutationKind.Identity, ParsePermutationKind);
        if (permutationKind is PermutationKind.ChunkPermutation or PermutationKind.Feistel)
        {
            throw new CliUsageException("The simplified CLI does not expose chunk permutation or Feistel scenarios. Use code/library scenarios for those cases.");
        }

        EmitterKind emitterKind = arguments.GetEnum("emitter", EmitterKind.Hex16, ParseEmitterKind);
        if (emitterKind == EmitterKind.CustomAlphabet)
        {
            throw new CliUsageException("The simplified CLI does not expose custom alphabets. Use code/library scenarios for those cases.");
        }
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

    private static NumberKind ParseNumberKind(string value) => value.ToLowerInvariant() switch
    {
        "uint32" => NumberKind.UInt32,
        "uint64" => NumberKind.UInt64,
        _ => throw new CliUsageException($"Unsupported number kind '{value}'.")
    };

    private static MixerKind ParseMixerKind(string value) => value.ToLowerInvariant() switch
    {
        "none" => MixerKind.None,
        "xor" => MixerKind.Xor,
        "add" => MixerKind.Add,
        "multiply" => MixerKind.Multiply,
        _ => throw new CliUsageException($"Unsupported mixer kind '{value}'.")
    };

    private static PermutationKind ParsePermutationKind(string value) => value.ToLowerInvariant() switch
    {
        "identity" => PermutationKind.Identity,
        "not" => PermutationKind.Not,
        "rotate" => PermutationKind.Rotate,
        "byteswap" => PermutationKind.ByteSwap,
        "bitreverse" => PermutationKind.BitReverse,
        "nibbleswap" => PermutationKind.NibbleSwap,
        "chunk" => PermutationKind.ChunkPermutation,
        "feistel" => PermutationKind.Feistel,
        _ => throw new CliUsageException($"Unsupported permutation kind '{value}'.")
    };

    private static EmitterKind ParseEmitterKind(string value) => value.ToLowerInvariant() switch
    {
        "hex16" => EmitterKind.Hex16,
        "base32" => EmitterKind.Base32Crockford,
        "base64url" => EmitterKind.Base64Url,
        "bytes" => EmitterKind.ByteArray,
        "custom" => EmitterKind.CustomAlphabet,
        _ => throw new CliUsageException($"Unsupported emitter kind '{value}'.")
    };

    private static AlphabetKind ParseAlphabetKind(string value) => value.ToLowerInvariant() switch
    {
        "none" => AlphabetKind.None,
        "hex16" => AlphabetKind.Hex16,
        "base32-crockford" => AlphabetKind.Base32Crockford,
        "base64url" => AlphabetKind.Base64Url,
        "custom" => AlphabetKind.Custom,
        _ => throw new CliUsageException($"Unsupported alphabet kind '{value}'.")
    };

    private static OutputKind ParseOutputKind(string value) => value.ToLowerInvariant() switch
    {
        "string" => OutputKind.String,
        "char-array" => OutputKind.CharArray,
        "byte-array" => OutputKind.ByteArray,
        _ => throw new CliUsageException($"Unsupported output kind '{value}'.")
    };

    private static ByteArrayTextFormat ParseByteArrayTextFormat(string value) => value.ToLowerInvariant() switch
    {
        "hex" => ByteArrayTextFormat.Hex,
        "base64" => ByteArrayTextFormat.Base64,
        "csv-decimal" => ByteArrayTextFormat.CommaSeparatedDecimal,
        _ => throw new CliUsageException($"Unsupported byte-array format '{value}'.")
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

    private static ulong ParseULongInvariant(string value, string flagName)
    {
        if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong parsed))
        {
            return parsed;
        }

        throw new CliUsageException($"Flag '--{flagName}' requires an unsigned integer value.");
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

    private static int NotImplemented(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  bpl encode --value <number> --bits <n> --chunk-size <n> --mix <kind> --permute <kind> --emitter <kind> [common flags]");
        Console.Error.WriteLine("  bpl decode --value <text-or-byte-array> --bits <n> --chunk-size <n> --mix <kind> --permute <kind> --emitter <kind> [common flags]");
        Console.Error.WriteLine("Common flags:");
        Console.Error.WriteLine("  --number-kind uint32|uint64");
        Console.Error.WriteLine("  --salt <ulong> | --salt-text <text>");
        Console.Error.WriteLine("  --alphabet none|hex16|base32-crockford|base64url");
        Console.Error.WriteLine("  --output-kind string|byte-array");
        Console.Error.WriteLine("  --byte-array-format hex|base64|csv-decimal");
        Console.Error.WriteLine("Notes:");
        Console.Error.WriteLine("  Advanced permutation/mutation/scenario shaping flags are intentionally not exposed by the simplified CLI.");
        Console.Error.WriteLine("  Use the library/code path for benchmarks and advanced scenarios.");
    }
}

internal sealed class CliArguments
{
    private readonly Dictionary<string, string> _values;

    private CliArguments(Dictionary<string, string> values)
    {
        _values = values;
    }

    public static CliArguments Parse(IEnumerable<string> args)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
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

            values[pendingKey] = arg;
            pendingKey = null;
        }

        if (pendingKey is not null)
        {
            throw new CliUsageException($"Missing value for '--{pendingKey}'.");
        }

        return new CliArguments(values);
    }

    public bool Contains(string key) => _values.ContainsKey(key);

    public bool TryGetValue(string key, out string? value) => _values.TryGetValue(key, out value);

    public string GetRequiredString(string key)
    {
        return _values.TryGetValue(key, out string? value)
            ? value
            : throw new CliUsageException($"Missing required flag '--{key}'.");
    }

    public string? GetOptionalString(string key) => _values.TryGetValue(key, out string? value) ? value : null;

    public int GetRequiredInt(string key)
    {
        string value = GetRequiredString(key);
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            return parsed;
        }

        throw new CliUsageException($"Flag '--{key}' requires an integer value.");
    }

    public int? GetOptionalInt(string key)
    {
        if (!_values.TryGetValue(key, out string? value))
        {
            return null;
        }

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

    public ulong? GetOptionalULong(string key)
    {
        if (!_values.TryGetValue(key, out string? value))
        {
            return null;
        }

        if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong parsed))
        {
            return parsed;
        }

        throw new CliUsageException($"Flag '--{key}' requires an unsigned integer value.");
    }

    public TEnum GetEnum<TEnum>(string key, TEnum defaultValue, Func<string, TEnum> parser)
    {
        return _values.TryGetValue(key, out string? value) ? parser(value) : defaultValue;
    }
}

internal sealed class CliUsageException : Exception
{
    public CliUsageException(string message) : base(message)
    {
    }
}
