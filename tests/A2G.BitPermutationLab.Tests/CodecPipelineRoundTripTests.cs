using A2G.BitPermutationLab.Core;
using A2G.BitPermutationLab.Binary;
using A2G.BitPermutationLab.Chunking;
using A2G.BitPermutationLab.Custom;
using A2G.BitPermutationLab.Emitters;
using A2G.BitPermutationLab.Mixers;
using A2G.BitPermutationLab.Permutations;

namespace A2G.BitPermutationLab.Tests;

public sealed class CodecPipelineRoundTripTests
{
    private readonly CodecPipeline _pipeline = new();

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(12345UL)]
    [InlineData(4294967295UL)]
    public void RoundTrips_HexString_WithXorAndRotate(ulong value)
    {
        CodecParameters parameters = CreateHexParameters();

        CodecResult encoded = _pipeline.Encode(value, parameters);
        DecodeResult decoded = _pipeline.Decode(encoded.StringValue!.AsSpan(), parameters);

        Assert.True(decoded.Success);
        Assert.Equal(value, decoded.Value);
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(12345UL)]
    [InlineData(18446744073709551615UL)]
    public void RoundTrips_ByteArray_WithIdentity(ulong value)
    {
        CodecParameters parameters = CreateByteArrayParameters();

        CodecResult encoded = _pipeline.Encode(value, parameters);
        DecodeResult decoded = _pipeline.Decode(encoded.ByteArrayValue!, parameters);

        Assert.True(decoded.Success);
        Assert.Equal(value, decoded.Value);
    }

    [Fact]
    public void RoundTrips_CustomBitMutation_AfterMix()
    {
        const string mutationName = "swap-low-high-16-test";
        CustomMutationRegistry.Register(new DelegateMutation(
            mutationName,
            static (value, _) => ((value & 0xFFFFUL) << 16) | ((value >> 16) & 0xFFFFUL),
            static (value, _) => ((value & 0xFFFFUL) << 16) | ((value >> 16) & 0xFFFFUL)));

        CodecParameters parameters = CreateHexParameters() with
        {
            CustomMutation = new CustomMutationParameters(
                mutationName,
                CustomMutationPosition.AfterMix,
                new Dictionary<string, string>())
        };

        CodecResult encoded = _pipeline.Encode(0x12345678UL, parameters);
        DecodeResult decoded = _pipeline.Decode(encoded.StringValue!.AsSpan(), parameters);

        Assert.True(decoded.Success);
        Assert.Equal(0x12345678UL, decoded.Value);
    }

    [Fact]
    public void RoundTrips_CustomChunkMutation_BeforeEmit()
    {
        const string mutationName = "reverse-chunks-test";
        CustomMutationRegistry.Register(new DelegateChunkMutation(
            mutationName,
            static (input, output, _) =>
            {
                for (int i = 0; i < input.Length; i++)
                {
                    output[i] = input[input.Length - 1 - i];
                }
            },
            static (input, output, _) =>
            {
                for (int i = 0; i < input.Length; i++)
                {
                    output[i] = input[input.Length - 1 - i];
                }
            }));

        CodecParameters parameters = CreateHexParameters() with
        {
            CustomChunkMutation = new CustomChunkMutationParameters(
                mutationName,
                CustomChunkMutationPosition.BeforeEmit,
                new Dictionary<string, string>())
        };

        CodecResult encoded = _pipeline.Encode(0x12345678UL, parameters);
        DecodeResult decoded = _pipeline.Decode(encoded.StringValue!.AsSpan(), parameters);

        Assert.True(decoded.Success);
        Assert.Equal(0x12345678UL, decoded.Value);
    }

    private static CodecParameters CreateHexParameters()
    {
        return new CodecParameters(
            "xor-rotate-hex32",
            NumberKind.UInt32,
            32,
            42UL,
            new BinaryParameters(BinaryKind.FixedUnsigned),
            new MixerParameters(MixerKind.Xor),
            new PermutationParameters(PermutationKind.Rotate, RotateBy: 11),
            new ChunkingParameters(ChunkerKind.Fixed, 4),
            new EmitterParameters(EmitterKind.Hex16, AlphabetKind.Hex16, OutputKind.String));
    }

    private static CodecParameters CreateByteArrayParameters()
    {
        return new CodecParameters(
            "identity-bytes-64",
            NumberKind.UInt64,
            64,
            0UL,
            new BinaryParameters(BinaryKind.FixedUnsigned),
            new MixerParameters(MixerKind.None),
            new PermutationParameters(PermutationKind.Identity),
            new ChunkingParameters(ChunkerKind.Fixed, 8),
            new EmitterParameters(EmitterKind.ByteArray, AlphabetKind.None, OutputKind.ByteArray));
    }
}
