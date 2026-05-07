using A2G.BitPermutationLab.Abstractions;
using A2G.BitPermutationLab.Binary;
using A2G.BitPermutationLab.Chunking;
using A2G.BitPermutationLab.Custom;
using A2G.BitPermutationLab.Emitters;
using A2G.BitPermutationLab.Mixers;
using A2G.BitPermutationLab.Permutations;
using A2G.BitPermutationLab.Validation;

namespace A2G.BitPermutationLab.Core;

public sealed class CodecPipeline : ICodecPipeline
{
    public CodecResult Encode(ulong value, CodecParameters parameters)
    {
        EnsureValidForEncode(value, parameters);

        ulong binary = FixedUnsignedBinary.ToBinary(value, parameters);
        ulong mixedInput = ApplyCustomMutationForward(binary, parameters, CustomMutationPosition.BeforeMix);
        ulong mixed = MixerFactory.Create(parameters.Mixer.Kind).Forward(mixedInput, parameters);
        ulong permutedInput = ApplyCustomMutationForward(mixed, parameters, CustomMutationPosition.AfterMix);
        ulong permuted = PermutationFactory.Create(parameters.Permutation.Kind).Forward(permutedInput, parameters);
        ulong chunkable = ApplyCustomMutationForward(permuted, parameters, CustomMutationPosition.AfterPermutation);

        int[] chunks = ChunkerFactory.Create(parameters.Chunking.Kind).Chunk(chunkable, parameters);
        int[] emittedChunks = ApplyCustomChunkMutationForward(chunks, parameters);

        return EmitterFactory.Create(parameters.Emitter.Kind).Emit(emittedChunks, parameters);
    }

    public DecodeResult Decode(ReadOnlySpan<char> value, CodecParameters parameters)
    {
        ValidationResult validation = ParameterValidator.Validate(parameters);
        if (!validation.IsValid)
        {
            return Failure(validation);
        }

        try
        {
            int[] emittedChunks = EmitterFactory.Create(parameters.Emitter.Kind).Read(value, parameters);
            return DecodeChunks(emittedChunks, parameters);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return new DecodeResult(0UL, false, exception.Message);
        }
    }

    public DecodeResult Decode(ReadOnlySpan<byte> value, CodecParameters parameters)
    {
        ValidationResult validation = ParameterValidator.Validate(parameters);
        if (!validation.IsValid)
        {
            return Failure(validation);
        }

        try
        {
            int[] emittedChunks = EmitterFactory.Create(parameters.Emitter.Kind).Read(value, parameters);
            return DecodeChunks(emittedChunks, parameters);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return new DecodeResult(0UL, false, exception.Message);
        }
    }

    private static DecodeResult DecodeChunks(int[] emittedChunks, CodecParameters parameters)
    {
        int[] chunks = ApplyCustomChunkMutationReverse(emittedChunks, parameters);
        ulong permuted = ChunkerFactory.Create(parameters.Chunking.Kind).Unchunk(chunks, parameters);
        ulong permutationInput = ApplyCustomMutationReverse(permuted, parameters, CustomMutationPosition.AfterPermutation);
        ulong mixed = PermutationFactory.Create(parameters.Permutation.Kind).Reverse(permutationInput, parameters);
        ulong mixInput = ApplyCustomMutationReverse(mixed, parameters, CustomMutationPosition.AfterMix);
        ulong binary = MixerFactory.Create(parameters.Mixer.Kind).Reverse(mixInput, parameters);
        ulong integerInput = ApplyCustomMutationReverse(binary, parameters, CustomMutationPosition.BeforeMix);
        ulong value = FixedUnsignedBinary.ToInteger(integerInput, parameters);

        return new DecodeResult(value, true);
    }

    private static void EnsureValidForEncode(ulong value, CodecParameters parameters)
    {
        ValidationResult parameterValidation = ParameterValidator.Validate(parameters);
        if (!parameterValidation.IsValid)
        {
            throw new InvalidOperationException(string.Join("; ", parameterValidation.Errors.Select(error => error.Message)));
        }

        ValidationResult valueValidation = ParameterValidator.ValidateValue(value, parameters);
        if (!valueValidation.IsValid)
        {
            throw new ArgumentOutOfRangeException(nameof(value), string.Join("; ", valueValidation.Errors.Select(error => error.Message)));
        }
    }

    private static ulong ApplyCustomMutationForward(ulong value, CodecParameters parameters, CustomMutationPosition position)
    {
        if (parameters.CustomMutation is null || parameters.CustomMutation.Position != position)
        {
            return BitMask.Apply(value, parameters.BitLength);
        }

        ICustomMutation mutation = ResolveCustomMutation(parameters.CustomMutation.Name);
        return BitMask.Apply(mutation.Forward(value, parameters), parameters.BitLength);
    }

    private static ulong ApplyCustomMutationReverse(ulong value, CodecParameters parameters, CustomMutationPosition position)
    {
        if (parameters.CustomMutation is null || parameters.CustomMutation.Position != position)
        {
            return BitMask.Apply(value, parameters.BitLength);
        }

        ICustomMutation mutation = ResolveCustomMutation(parameters.CustomMutation.Name);
        return BitMask.Apply(mutation.Reverse(value, parameters), parameters.BitLength);
    }

    private static int[] ApplyCustomChunkMutationForward(int[] chunks, CodecParameters parameters)
    {
        if (parameters.CustomChunkMutation is null)
        {
            return chunks;
        }

        ICustomChunkMutation mutation = ResolveCustomChunkMutation(parameters.CustomChunkMutation.Name);
        int[] output = new int[chunks.Length];
        mutation.Forward(chunks, output, parameters);
        return output;
    }

    private static int[] ApplyCustomChunkMutationReverse(int[] chunks, CodecParameters parameters)
    {
        if (parameters.CustomChunkMutation is null)
        {
            return chunks;
        }

        ICustomChunkMutation mutation = ResolveCustomChunkMutation(parameters.CustomChunkMutation.Name);
        int[] output = new int[chunks.Length];
        mutation.Reverse(chunks, output, parameters);
        return output;
    }

    private static ICustomMutation ResolveCustomMutation(string name)
    {
        return CustomMutationRegistry.TryGetMutation(name, out ICustomMutation mutation)
            ? mutation
            : throw new InvalidOperationException($"Custom mutation '{name}' is not registered.");
    }

    private static ICustomChunkMutation ResolveCustomChunkMutation(string name)
    {
        return CustomMutationRegistry.TryGetChunkMutation(name, out ICustomChunkMutation mutation)
            ? mutation
            : throw new InvalidOperationException($"Custom chunk mutation '{name}' is not registered.");
    }

    private static DecodeResult Failure(ValidationResult validation)
    {
        return new DecodeResult(
            0UL,
            false,
            string.Join("; ", validation.Errors.Select(error => error.Message)));
    }
}
