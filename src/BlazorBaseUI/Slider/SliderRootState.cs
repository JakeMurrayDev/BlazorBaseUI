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
    public static SliderRootState Default { get; } = new(
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

    public static SliderRootState FromFieldState(
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

    internal Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>();

        if (Dragging)
            attributes[SliderDataAttribute.Dragging.ToDataAttributeString()] = string.Empty;

        attributes[SliderDataAttribute.Orientation.ToDataAttributeString()] = Orientation.ToDataAttributeString() ?? "horizontal";

        if (Disabled)
            attributes[SliderDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        if (ReadOnly)
            attributes[SliderDataAttribute.ReadOnly.ToDataAttributeString()] = string.Empty;

        if (Required)
            attributes[SliderDataAttribute.Required.ToDataAttributeString()] = string.Empty;

        if (Valid == true)
            attributes[SliderDataAttribute.Valid.ToDataAttributeString()] = string.Empty;
        else if (Valid == false)
            attributes[SliderDataAttribute.Invalid.ToDataAttributeString()] = string.Empty;

        if (Touched)
            attributes[SliderDataAttribute.Touched.ToDataAttributeString()] = string.Empty;

        if (Dirty)
            attributes[SliderDataAttribute.Dirty.ToDataAttributeString()] = string.Empty;

        if (Focused)
            attributes[SliderDataAttribute.Focused.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}
