using A2G.BitPermutationLab.Abstractions;
using A2G.BitPermutationLab.Core;

namespace A2G.BitPermutationLab.Validation;

public static class RoundTripValidator
{
    public static ValidationResult ValidateParameters(CodecParameters parameters)
    {
        return ParameterValidator.Validate(parameters);
    }

    public static ValidationResult ValidateValues(IEnumerable<ulong> values, CodecParameters parameters)
    {
        List<ValidationError> errors = [];

        foreach (ulong value in values)
        {
            ValidationResult validation = ParameterValidator.ValidateValue(value, parameters);
            if (!validation.IsValid)
            {
                errors.AddRange(validation.Errors);
            }
        }

        return errors.Count == 0
            ? ValidationResult.Success
            : new ValidationResult(false, errors);
    }

    public static ValidationResult ValidateCustomMutation(
        ICustomMutation mutation,
        IEnumerable<ulong> values,
        CodecParameters parameters)
    {
        List<ValidationError> errors = [];

        foreach (ulong value in values)
        {
            ulong forward = mutation.Forward(value, parameters);
            ulong reverse = mutation.Reverse(forward, parameters);
            if (BitMask.Apply(reverse, parameters.BitLength) != BitMask.Apply(value, parameters.BitLength))
            {
                errors.Add(new ValidationError(
                    "CustomMutation",
                    $"Custom mutation '{mutation.Name}' failed round-trip validation for value '{value}'."));
                break;
            }
        }

        return errors.Count == 0
            ? ValidationResult.Success
            : new ValidationResult(false, errors);
    }
}
