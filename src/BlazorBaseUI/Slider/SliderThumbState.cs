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
    public static SliderThumbState Default { get; } = new(
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

    public static SliderThumbState FromRootState(SliderRootState rootState, int index, bool isActive) => new(
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

    internal Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>
        {
            [SliderThumbDataAttribute.Index.ToDataAttributeString()] = Index.ToString()
        };

        if (Dragging)
            attributes[SliderThumbDataAttribute.Dragging.ToDataAttributeString()] = string.Empty;

        attributes[SliderThumbDataAttribute.Orientation.ToDataAttributeString()] = Orientation.ToDataAttributeString() ?? "horizontal";

        if (Disabled)
            attributes[SliderThumbDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        if (ReadOnly)
            attributes[SliderThumbDataAttribute.ReadOnly.ToDataAttributeString()] = string.Empty;

        if (Required)
            attributes[SliderThumbDataAttribute.Required.ToDataAttributeString()] = string.Empty;

        if (Valid == true)
            attributes[SliderThumbDataAttribute.Valid.ToDataAttributeString()] = string.Empty;
        else if (Valid == false)
            attributes[SliderThumbDataAttribute.Invalid.ToDataAttributeString()] = string.Empty;

        if (Touched)
            attributes[SliderThumbDataAttribute.Touched.ToDataAttributeString()] = string.Empty;

        if (Dirty)
            attributes[SliderThumbDataAttribute.Dirty.ToDataAttributeString()] = string.Empty;

        if (Focused)
            attributes[SliderThumbDataAttribute.Focused.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}
