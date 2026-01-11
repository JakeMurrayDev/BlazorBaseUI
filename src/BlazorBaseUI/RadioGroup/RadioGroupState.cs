using BlazorBaseUI.Field;

namespace BlazorBaseUI.RadioGroup;

public sealed record RadioGroupState(
    bool Disabled,
    bool ReadOnly,
    bool Required,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    internal static RadioGroupState Default { get; } = new(
        Disabled: false,
        ReadOnly: false,
        Required: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    internal static RadioGroupState FromFieldState(
        FieldRootState fieldState,
        bool isDisabled,
        bool isReadOnly,
        bool isRequired) => new(
            Disabled: isDisabled,
            ReadOnly: isReadOnly,
            Required: isRequired,
            Valid: fieldState.Valid,
            Touched: fieldState.Touched,
            Dirty: fieldState.Dirty,
            Filled: fieldState.Filled,
            Focused: fieldState.Focused);
}
