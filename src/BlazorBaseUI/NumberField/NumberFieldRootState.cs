using BlazorBaseUI.Field;

namespace BlazorBaseUI.NumberField;

public readonly record struct NumberFieldRootState(
    double? Value,
    string InputValue,
    bool Required,
    bool Disabled,
    bool ReadOnly,
    bool Scrubbing,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    internal static NumberFieldRootState Default => new(
        Value: null,
        InputValue: string.Empty,
        Required: false,
        Disabled: false,
        ReadOnly: false,
        Scrubbing: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    internal static NumberFieldRootState FromFieldState(
        FieldRootState fieldState,
        double? value,
        string inputValue,
        bool required,
        bool disabled,
        bool readOnly,
        bool scrubbing) => new(
            Value: value,
            InputValue: inputValue,
            Required: required,
            Disabled: disabled,
            ReadOnly: readOnly,
            Scrubbing: scrubbing,
            Valid: fieldState.Valid,
            Touched: fieldState.Touched,
            Dirty: fieldState.Dirty,
            Filled: fieldState.Filled,
            Focused: fieldState.Focused);
}
