using BlazorBaseUI.Field;

namespace BlazorBaseUI.Switch;

public record SwitchRootState(
    bool Checked,
    bool Disabled,
    bool ReadOnly,
    bool Required,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    public static SwitchRootState Default { get; } = new(
        Checked: false,
        Disabled: false,
        ReadOnly: false,
        Required: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    public static SwitchRootState FromFieldState(
        FieldRootState fieldState,
        bool isChecked,
        bool isDisabled,
        bool isReadOnly,
        bool isRequired) => new(
            Checked: isChecked,
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

        if (Checked)
            attributes[SwitchDataAttribute.Checked.ToDataAttributeString()] = string.Empty;
        else
            attributes[SwitchDataAttribute.Unchecked.ToDataAttributeString()] = string.Empty;

        if (Disabled)
            attributes[SwitchDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        if (ReadOnly)
            attributes[SwitchDataAttribute.ReadOnly.ToDataAttributeString()] = string.Empty;

        if (Required)
            attributes[SwitchDataAttribute.Required.ToDataAttributeString()] = string.Empty;

        if (Valid == true)
            attributes[SwitchDataAttribute.Valid.ToDataAttributeString()] = string.Empty;
        else if (Valid == false)
            attributes[SwitchDataAttribute.Invalid.ToDataAttributeString()] = string.Empty;

        if (Touched)
            attributes[SwitchDataAttribute.Touched.ToDataAttributeString()] = string.Empty;

        if (Dirty)
            attributes[SwitchDataAttribute.Dirty.ToDataAttributeString()] = string.Empty;

        if (Filled)
            attributes[SwitchDataAttribute.Filled.ToDataAttributeString()] = string.Empty;

        if (Focused)
            attributes[SwitchDataAttribute.Focused.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}