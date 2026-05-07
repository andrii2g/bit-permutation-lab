using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Binary;

public sealed record BinaryParameters(
    BinaryKind Kind,
    BitOrderKind BitOrder = BitOrderKind.MsbFirst,
    ByteOrderKind ByteOrder = ByteOrderKind.BigEndian);
