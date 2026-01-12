using BlazorBaseUI.Field;

namespace BlazorBaseUI.Input;

public readonly record struct InputState(
    bool Disabled,
    bool Touched,
    bool Dirty,
    bool? Valid,
    bool Filled,
    bool Focused)
{
    internal static InputState Default => new(false, false, false, null, false, false);

    internal static InputState FromFieldRootState(FieldRootState state) =>
        new(state.Disabled, state.Touched, state.Dirty, state.Valid, state.Filled, state.Focused);

    public static implicit operator InputState(FieldRootState state) => FromFieldRootState(state);
}
