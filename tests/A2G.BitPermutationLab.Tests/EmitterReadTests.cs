using A2G.BitPermutationLab.Core;
using A2G.BitPermutationLab.Emitters;

namespace A2G.BitPermutationLab.Tests;

public sealed class EmitterReadTests
{
    [Fact]
    public void Rejects_Invalid_Hex_Character()
    {
        IEmitter emitter = new HexEmitter();
        CodecParameters parameters = new(
            "hex-read-test",
            NumberKind.UInt32,
            32,
            0UL,
            new Binary.BinaryParameters(BinaryKind.FixedUnsigned),
            new Mixers.MixerParameters(MixerKind.None),
            new Permutations.PermutationParameters(PermutationKind.Identity),
            new Chunking.ChunkingParameters(ChunkerKind.Fixed, 4),
            new EmitterParameters(EmitterKind.Hex16, AlphabetKind.Hex16, OutputKind.String));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => emitter.Read("12G4".AsSpan(), parameters));

        Assert.Contains("not valid", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
