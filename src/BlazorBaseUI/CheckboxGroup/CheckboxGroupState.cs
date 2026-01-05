using BlazorBaseUI.Field;

namespace BlazorBaseUI.CheckboxGroup;

public readonly record struct CheckboxGroupState(
    bool Disabled,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    internal static CheckboxGroupState Default { get; } = new(
        Disabled: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    internal static CheckboxGroupState FromFieldState(
        FieldRootState fieldState,
        bool isDisabled) => new(
            Disabled: isDisabled,
            Valid: fieldState.Valid,
            Touched: fieldState.Touched,
            Dirty: fieldState.Dirty,
            Filled: fieldState.Filled,
            Focused: fieldState.Focused);
}
