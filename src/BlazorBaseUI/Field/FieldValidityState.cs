namespace BlazorBaseUI.Field;

public sealed record FieldValidityState(
    bool BadInput = false,
    bool CustomError = false,
    bool PatternMismatch = false,
    bool RangeOverflow = false,
    bool RangeUnderflow = false,
    bool StepMismatch = false,
    bool TooLong = false,
    bool TooShort = false,
    bool TypeMismatch = false,
    bool ValueMissing = false,
    bool? Valid = null)
{
    internal static FieldValidityState Default { get; } = new();
}
