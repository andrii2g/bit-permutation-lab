using A2G.BitPermutationLab.Binary;
using A2G.BitPermutationLab.Chunking;
using A2G.BitPermutationLab.Core;
using A2G.BitPermutationLab.Emitters;
using A2G.BitPermutationLab.Mixers;
using A2G.BitPermutationLab.Permutations;

namespace A2G.BitPermutationLab.Benchmarking;

public static class BenchmarkProfileFactory
{
    public static IReadOnlyList<BenchmarkScenario> Create(BenchmarkProfileKind profileKind)
    {
        return profileKind switch
        {
            BenchmarkProfileKind.Quick => CreateQuick(),
            BenchmarkProfileKind.Default => CreateDefault(),
            BenchmarkProfileKind.Full => CreateFull(),
            _ => throw new ArgumentOutOfRangeException(nameof(profileKind), profileKind, "Unsupported benchmark profile.")
        };
    }

    private static IReadOnlyList<BenchmarkScenario> CreateQuick()
    {
        return
        [
            new BenchmarkScenario(
                "xor-rotate-hex32",
                CreateParameters(
                    "xor-rotate-hex32",
                    NumberKind.UInt32,
                    32,
                    42UL,
                    new MixerParameters(MixerKind.Xor),
                    new PermutationParameters(PermutationKind.Rotate, RotateBy: 11),
                    new ChunkingParameters(ChunkerKind.Fixed, 4),
                    new EmitterParameters(EmitterKind.Hex16, AlphabetKind.Hex16, OutputKind.String)),
                [1UL, 2UL, 1000UL, 10000UL, 1000000UL]),
            new BenchmarkScenario(
                "identity-bytes-64",
                CreateParameters(
                    "identity-bytes-64",
                    NumberKind.UInt64,
                    64,
                    0UL,
                    new MixerParameters(MixerKind.None),
                    new PermutationParameters(PermutationKind.Identity),
                    new ChunkingParameters(ChunkerKind.Fixed, 8),
                    new EmitterParameters(EmitterKind.ByteArray, AlphabetKind.None, OutputKind.ByteArray)),
                [1UL, 2UL, 1000UL, 10000UL, 1000000UL])
        ];
    }

    private static IReadOnlyList<BenchmarkScenario> CreateDefault()
    {
        return
        [
            ..CreateQuick(),
            new BenchmarkScenario(
                "add-bitswap-base64-48",
                CreateParameters(
                    "add-bitswap-base64-48",
                    NumberKind.UInt64,
                    48,
                    123UL,
                    new MixerParameters(MixerKind.Add),
                    new PermutationParameters(PermutationKind.BitReverse),
                    new ChunkingParameters(ChunkerKind.Fixed, 6),
                    new EmitterParameters(EmitterKind.Base64Url, AlphabetKind.Base64Url, OutputKind.String)),
                [1UL, 1000UL, 1000000UL]),
            new BenchmarkScenario(
                "multiply-byteswap-base32-40",
                CreateParameters(
                    "multiply-byteswap-base32-40",
                    NumberKind.UInt64,
                    40,
                    99UL,
                    new MixerParameters(MixerKind.Multiply),
                    new PermutationParameters(PermutationKind.ByteSwap),
                    new ChunkingParameters(ChunkerKind.Fixed, 5),
                    new EmitterParameters(EmitterKind.Base32Crockford, AlphabetKind.Base32Crockford, OutputKind.String)),
                [1UL, 2UL, 1000UL, 10000UL])
        ];
    }

    private static IReadOnlyList<BenchmarkScenario> CreateFull()
    {
        return
        [
            ..CreateDefault(),
            new BenchmarkScenario(
                "nibbleswap-hex32",
                CreateParameters(
                    "nibbleswap-hex32",
                    NumberKind.UInt32,
                    32,
                    0UL,
                    new MixerParameters(MixerKind.None),
                    new PermutationParameters(PermutationKind.NibbleSwap, NibbleSwap: NibbleSwapKind.ReverseNibbles),
                    new ChunkingParameters(ChunkerKind.Fixed, 4),
                    new EmitterParameters(EmitterKind.Hex16, AlphabetKind.Hex16, OutputKind.String)),
                [1UL, 255UL, 1000UL]),
            new BenchmarkScenario(
                "chunkperm-base32-40",
                CreateParameters(
                    "chunkperm-base32-40",
                    NumberKind.UInt64,
                    40,
                    321UL,
                    new MixerParameters(MixerKind.Add),
                    new PermutationParameters(
                        PermutationKind.ChunkPermutation,
                        ChunkPermutationGroupSize: 5,
                        ChunkPermutationVariant: ChunkPermutationVariant.ReverseGroups),
                    new ChunkingParameters(ChunkerKind.Fixed, 5),
                    new EmitterParameters(EmitterKind.Base32Crockford, AlphabetKind.Base32Crockford, OutputKind.String)),
                [1UL, 2UL, 1000UL]),
            new BenchmarkScenario(
                "feistel-base64-48",
                CreateParameters(
                    "feistel-base64-48",
                    NumberKind.UInt64,
                    48,
                    777UL,
                    new MixerParameters(MixerKind.Xor),
                    new PermutationParameters(
                        PermutationKind.Feistel,
                        FeistelRounds: 2,
                        FeistelRoundFunction: FeistelRoundFunctionKind.XorShiftAdd),
                    new ChunkingParameters(ChunkerKind.Fixed, 6),
                    new EmitterParameters(EmitterKind.Base64Url, AlphabetKind.Base64Url, OutputKind.String)),
                [1UL, 2UL, 10000UL])
        ];
    }

    private static CodecParameters CreateParameters(
        string name,
        NumberKind numberKind,
        int bitLength,
        ulong saltSeed,
        MixerParameters mixer,
        PermutationParameters permutation,
        ChunkingParameters chunking,
        EmitterParameters emitter)
    {
        return new CodecParameters(
            name,
            numberKind,
            bitLength,
            saltSeed,
            new BinaryParameters(BinaryKind.FixedUnsigned),
            mixer,
            permutation,
            chunking,
            emitter);
    }
}
