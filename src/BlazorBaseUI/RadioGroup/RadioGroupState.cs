using BlazorBaseUI.Field;

namespace BlazorBaseUI.RadioGroup;

public record RadioGroupState(
    bool Disabled,
    bool ReadOnly,
    bool Required,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    public static RadioGroupState Default { get; } = new(
        Disabled: false,
        ReadOnly: false,
        Required: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    public static RadioGroupState FromFieldState(
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

    public Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>();

        if (Disabled)
            attributes[RadioGroupDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        if (ReadOnly)
            attributes[RadioGroupDataAttribute.ReadOnly.ToDataAttributeString()] = string.Empty;

        if (Required)
            attributes[RadioGroupDataAttribute.Required.ToDataAttributeString()] = string.Empty;

        if (Valid == true)
            attributes[RadioGroupDataAttribute.Valid.ToDataAttributeString()] = string.Empty;
        else if (Valid == false)
            attributes[RadioGroupDataAttribute.Invalid.ToDataAttributeString()] = string.Empty;

        if (Touched)
            attributes[RadioGroupDataAttribute.Touched.ToDataAttributeString()] = string.Empty;

        if (Dirty)
            attributes[RadioGroupDataAttribute.Dirty.ToDataAttributeString()] = string.Empty;

        if (Filled)
            attributes[RadioGroupDataAttribute.Filled.ToDataAttributeString()] = string.Empty;

        if (Focused)
            attributes[RadioGroupDataAttribute.Focused.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}
