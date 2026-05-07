using A2G.BitPermutationLab.Core;
using A2G.BitPermutationLab.Emitters;
using A2G.BitPermutationLab.Permutations;

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

        if (parameters.Binary.Kind != BinaryKind.FixedUnsigned)
        {
            errors.Add(new ValidationError("BinaryKind", "Only FixedUnsigned binary kind is supported."));
        }

        if (parameters.NumberKind == NumberKind.UInt32 && parameters.BitLength > 32)
        {
            errors.Add(new ValidationError("NumberKind", "UInt32 scenarios cannot exceed 32 bits."));
        }

        if (parameters.Chunking.Kind != ChunkerKind.Fixed)
        {
            errors.Add(new ValidationError("ChunkerKind", "Only Fixed chunking is supported."));
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

        ValidateMixer(parameters, errors);
        ValidatePermutation(parameters, errors);
        ValidateEmitter(parameters, errors);

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

    private static void ValidateMixer(CodecParameters parameters, List<ValidationError> errors)
    {
        switch (parameters.Mixer.Kind)
        {
            case MixerKind.None:
                if (parameters.Mixer.LiteralMask is not null ||
                    parameters.Mixer.LiteralAddend is not null ||
                    parameters.Mixer.Multiplier is not null)
                {
                    errors.Add(new ValidationError("MixerKind", "MixerKind.None does not accept mixer-specific fields."));
                }

                break;
            case MixerKind.Xor:
                if (parameters.Mixer.LiteralAddend is not null || parameters.Mixer.Multiplier is not null)
                {
                    errors.Add(new ValidationError("MixerKind", "Xor mixer does not accept addend or multiplier fields."));
                }

                break;
            case MixerKind.Add:
                if (parameters.Mixer.LiteralMask is not null || parameters.Mixer.Multiplier is not null)
                {
                    errors.Add(new ValidationError("MixerKind", "Add mixer does not accept mask or multiplier fields."));
                }

                break;
            case MixerKind.Multiply:
                if (parameters.Mixer.LiteralMask is not null || parameters.Mixer.LiteralAddend is not null)
                {
                    errors.Add(new ValidationError("MixerKind", "Multiply mixer does not accept mask or addend fields."));
                }

                if (parameters.Mixer.Multiplier is ulong multiplier && multiplier % 2 == 0)
                {
                    errors.Add(new ValidationError("Multiplier", "Multiply mixer multiplier must be odd."));
                }

                break;
        }
    }

    private static void ValidatePermutation(CodecParameters parameters, List<ValidationError> errors)
    {
        PermutationParameters permutation = parameters.Permutation;

        switch (permutation.Kind)
        {
            case PermutationKind.Identity:
            case PermutationKind.Not:
            case PermutationKind.BitReverse:
                ValidateNoPermutationSpecificFields(permutation, errors);
                break;
            case PermutationKind.Rotate:
                if (permutation.RotateBy is null)
                {
                    errors.Add(new ValidationError("RotateBy", "Rotate permutation requires RotateBy."));
                }

                break;
            case PermutationKind.ByteSwap:
                if (parameters.BitLength % 8 != 0)
                {
                    errors.Add(new ValidationError("ByteSwap", "ByteSwap requires a bit length divisible by 8."));
                }

                break;
            case PermutationKind.NibbleSwap:
                if (parameters.BitLength % 4 != 0)
                {
                    errors.Add(new ValidationError("NibbleSwap", "NibbleSwap requires a bit length divisible by 4."));
                }

                if (permutation.NibbleSwap is null)
                {
                    errors.Add(new ValidationError("NibbleSwap", "NibbleSwap permutation requires a nibble swap mode."));
                }
                else if (permutation.NibbleSwap == NibbleSwapKind.SwapAdjacentNibbles &&
                         ((parameters.BitLength / 4) % 2 != 0))
                {
                    errors.Add(new ValidationError("NibbleSwap", "SwapAdjacentNibbles requires an even nibble count."));
                }

                break;
            case PermutationKind.ChunkPermutation:
                ValidateChunkPermutation(parameters, errors);
                break;
            case PermutationKind.Feistel:
                if (parameters.BitLength % 2 != 0)
                {
                    errors.Add(new ValidationError("Feistel", "Feistel requires an even bit length."));
                }

                if (permutation.FeistelRounds is null || permutation.FeistelRounds is < 1 or > 4)
                {
                    errors.Add(new ValidationError("FeistelRounds", "Feistel rounds must be between 1 and 4."));
                }

                if (permutation.FeistelRoundFunction is null)
                {
                    errors.Add(new ValidationError("FeistelRoundFunction", "Feistel requires a round function."));
                }

                break;
        }
    }

    private static void ValidateChunkPermutation(CodecParameters parameters, List<ValidationError> errors)
    {
        PermutationParameters permutation = parameters.Permutation;

        if (permutation.ChunkPermutationGroupSize is null)
        {
            errors.Add(new ValidationError("ChunkPermutationGroupSize", "ChunkPermutation requires a group size."));
            return;
        }

        int groupSize = permutation.ChunkPermutationGroupSize.Value;
        if (groupSize < 1 || groupSize > 32)
        {
            errors.Add(new ValidationError("ChunkPermutationGroupSize", "ChunkPermutation group size must be between 1 and 32."));
        }

        if (parameters.BitLength % groupSize != 0)
        {
            errors.Add(new ValidationError("ChunkPermutationGroupSize", "ChunkPermutation group size must divide bit length exactly."));
        }

        if (permutation.ChunkPermutationVariant is null)
        {
            errors.Add(new ValidationError("ChunkPermutationVariant", "ChunkPermutation requires a permutation variant."));
            return;
        }

        int groupCount = parameters.BitLength / groupSize;
        switch (permutation.ChunkPermutationVariant)
        {
            case ChunkPermutationVariant.ExplicitOrder:
                if (permutation.ChunkPermutationOrder is null ||
                    permutation.ChunkPermutationOrder.Count != groupCount ||
                    permutation.ChunkPermutationOrder.Order().SequenceEqual(Enumerable.Range(0, groupCount)) is false)
                {
                    errors.Add(new ValidationError("ChunkPermutationOrder", "ChunkPermutation explicit order must be a complete zero-based permutation."));
                }

                break;
            case ChunkPermutationVariant.RotateGroupsLeft:
            case ChunkPermutationVariant.RotateGroupsRight:
                if (permutation.ChunkPermutationRotateBy is null)
                {
                    errors.Add(new ValidationError("ChunkPermutationRotateBy", "ChunkPermutation rotate variants require a rotate amount."));
                }

                break;
            case ChunkPermutationVariant.SwapAdjacentGroups:
                if (groupCount % 2 != 0)
                {
                    errors.Add(new ValidationError("ChunkPermutationVariant", "SwapAdjacentGroups requires an even group count."));
                }

                break;
        }
    }

    private static void ValidateEmitter(CodecParameters parameters, List<ValidationError> errors)
    {
        EmitterParameters emitter = parameters.Emitter;

        switch (emitter.Kind)
        {
            case EmitterKind.Hex16:
                ValidateCharEmitter(parameters, errors, 4, AlphabetKind.Hex16, "Hex16");
                break;
            case EmitterKind.Base32Crockford:
                ValidateCharEmitter(parameters, errors, 5, AlphabetKind.Base32Crockford, "Base32Crockford");
                break;
            case EmitterKind.Base64Url:
                ValidateCharEmitter(parameters, errors, 6, AlphabetKind.Base64Url, "Base64Url");
                break;
            case EmitterKind.ByteArray:
                if (parameters.Chunking.ChunkSize != 8)
                {
                    errors.Add(new ValidationError("Emitter", "ByteArray emitter requires chunk size 8."));
                }

                if (emitter.AlphabetKind != AlphabetKind.None)
                {
                    errors.Add(new ValidationError("Emitter", "ByteArray emitter requires AlphabetKind.None."));
                }

                if (emitter.OutputKind != OutputKind.ByteArray)
                {
                    errors.Add(new ValidationError("Emitter", "ByteArray emitter requires OutputKind.ByteArray."));
                }

                break;
            case EmitterKind.CustomAlphabet:
                if (emitter.CustomAlphabet is null)
                {
                    errors.Add(new ValidationError("Emitter", "CustomAlphabet emitter requires a custom alphabet."));
                    break;
                }

                int expectedLength = 1 << parameters.Chunking.ChunkSize;
                if (emitter.CustomAlphabet.Length != expectedLength)
                {
                    errors.Add(new ValidationError("Emitter", $"Custom alphabet must contain exactly {expectedLength} characters."));
                }

                if (emitter.CustomAlphabet.Distinct().Count() != emitter.CustomAlphabet.Length)
                {
                    errors.Add(new ValidationError("Emitter", "Custom alphabet characters must be unique."));
                }

                if (emitter.OutputKind == OutputKind.ByteArray)
                {
                    errors.Add(new ValidationError("Emitter", "Custom alphabet emitter supports only string or char-array output."));
                }

                break;
        }
    }

    private static void ValidateCharEmitter(
        CodecParameters parameters,
        List<ValidationError> errors,
        int requiredChunkSize,
        AlphabetKind requiredAlphabet,
        string emitterName)
    {
        if (parameters.Chunking.ChunkSize != requiredChunkSize)
        {
            errors.Add(new ValidationError("Emitter", $"{emitterName} emitter requires chunk size {requiredChunkSize}."));
        }

        if (parameters.Emitter.AlphabetKind != requiredAlphabet)
        {
            errors.Add(new ValidationError("Emitter", $"{emitterName} emitter requires alphabet {requiredAlphabet}."));
        }

        if (parameters.Emitter.OutputKind == OutputKind.ByteArray)
        {
            errors.Add(new ValidationError("Emitter", $"{emitterName} emitter supports only string or char-array output."));
        }
    }

    private static void ValidateNoPermutationSpecificFields(PermutationParameters permutation, List<ValidationError> errors)
    {
        if (permutation.RotateBy is not null ||
            permutation.NibbleSwap is not null ||
            permutation.ChunkPermutationGroupSize is not null ||
            permutation.ChunkPermutationVariant is not null ||
            permutation.ChunkPermutationOrder is not null ||
            permutation.ChunkPermutationRotateBy is not null ||
            permutation.FeistelRounds is not null ||
            permutation.FeistelRoundFunction is not null)
        {
            errors.Add(new ValidationError("PermutationKind", $"{permutation.Kind} does not accept permutation-specific fields."));
        }
    }
}
