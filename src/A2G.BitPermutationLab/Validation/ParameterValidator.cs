using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Validation;

public static class ParameterValidator
{
    private static readonly HashSet<int> SupportedBitLengths = new([8, 16, 24, 32, 40, 48, 56, 64]);
    private static readonly HashSet<int> SupportedChunkSizes = new([4, 5, 6, 8]);

    public static ValidationResult Validate(CodecParameters parameters)
    {
        List<ValidationError> errors = [];

        if (!SupportedBitLengths.Contains(parameters.BitLength))
        {
            errors.Add(new ValidationError("BitLength", $"Unsupported bit length '{parameters.BitLength}'."));
        }

        if (parameters.NumberKind == NumberKind.UInt32 && parameters.BitLength > 32)
        {
            errors.Add(new ValidationError("NumberKind", "UInt32 scenarios cannot exceed 32 bits."));
        }

        if (!SupportedChunkSizes.Contains(parameters.Chunking.ChunkSize))
        {
            errors.Add(new ValidationError("ChunkSize", $"Unsupported chunk size '{parameters.Chunking.ChunkSize}'."));
        }
        else if (parameters.BitLength > 0 && parameters.BitLength % parameters.Chunking.ChunkSize != 0)
        {
            errors.Add(new ValidationError("ChunkSize", "Chunk size must divide bit length exactly."));
        }

        if (parameters.Binary.ByteOrder == ByteOrderKind.LittleEndian && parameters.BitLength % 8 != 0)
        {
            errors.Add(new ValidationError("ByteOrder", "LittleEndian requires a bit length divisible by 8."));
        }

        if (parameters.Mixer.Kind == MixerKind.Multiply && parameters.Mixer.Multiplier is ulong multiplier && multiplier % 2 == 0)
        {
            errors.Add(new ValidationError("Multiplier", "Multiply mixer multiplier must be odd."));
        }

        return errors.Count == 0
            ? ValidationResult.Success
            : new ValidationResult(false, errors);
    }

    public static ValidationResult ValidateValue(ulong value, CodecParameters parameters)
    {
        ulong mask = BitMask.ForBitLength(parameters.BitLength);
        return (value & ~mask) == 0
            ? ValidationResult.Success
            : ValidationResult.Failure(new ValidationError("Value", "Input value does not fit into the configured bit length."));
    }
}
