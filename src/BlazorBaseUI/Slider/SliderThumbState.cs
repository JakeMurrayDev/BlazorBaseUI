namespace BlazorBaseUI.Slider;

public sealed record SliderThumbState(
    int Index,
    bool Disabled,
    bool Dragging,
    Orientation Orientation,
    bool ReadOnly,
    bool Required,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Focused)
{
    internal static SliderThumbState Default { get; } = new(
        Index: 0,
        Disabled: false,
        Dragging: false,
        Orientation: Orientation.Horizontal,
        ReadOnly: false,
        Required: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Focused: false);

    internal static SliderThumbState FromRootState(SliderRootState rootState, int index, bool isActive) => new(
        Index: index,
        Disabled: rootState.Disabled,
        Dragging: rootState.Dragging && rootState.ActiveThumbIndex == index,
        Orientation: rootState.Orientation,
        ReadOnly: rootState.ReadOnly,
        Required: rootState.Required,
        Valid: rootState.Valid,
        Touched: rootState.Touched,
        Dirty: rootState.Dirty,
        Focused: isActive);
}
