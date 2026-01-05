namespace BlazorBaseUI.Field;

public readonly record struct FieldRootState(
    bool Disabled,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    internal static FieldRootState Default { get; } = new(
        Disabled: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);
}
