using BlazorBaseUI.Field;

namespace BlazorBaseUI.Slider;

public sealed record SliderRootState(
    int ActiveThumbIndex,
    bool Disabled,
    bool Dragging,
    double Max,
    double Min,
    int MinStepsBetweenValues,
    Orientation Orientation,
    bool ReadOnly,
    bool Required,
    double Step,
    double[] Values,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    internal static SliderRootState Default { get; } = new(
        ActiveThumbIndex: -1,
        Disabled: false,
        Dragging: false,
        Max: 100,
        Min: 0,
        MinStepsBetweenValues: 0,
        Orientation: Orientation.Horizontal,
        ReadOnly: false,
        Required: false,
        Step: 1,
        Values: [0],
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    internal static SliderRootState FromFieldState(
        FieldRootState fieldState,
        int activeThumbIndex,
        bool disabled,
        bool dragging,
        double max,
        double min,
        int minStepsBetweenValues,
        Orientation orientation,
        bool readOnly,
        bool required,
        double step,
        double[] values) => new(
            ActiveThumbIndex: activeThumbIndex,
            Disabled: disabled,
            Dragging: dragging,
            Max: max,
            Min: min,
            MinStepsBetweenValues: minStepsBetweenValues,
            Orientation: orientation,
            ReadOnly: readOnly,
            Required: required,
            Step: step,
            Values: values,
            Valid: fieldState.Valid,
            Touched: fieldState.Touched,
            Dirty: fieldState.Dirty,
            Filled: fieldState.Filled,
            Focused: fieldState.Focused);
}
