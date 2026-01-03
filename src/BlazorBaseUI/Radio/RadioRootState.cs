using BlazorBaseUI.Field;

namespace BlazorBaseUI.Radio;

public sealed record RadioRootState(
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
    public static RadioRootState Default { get; } = new(
        Checked: false,
        Disabled: false,
        ReadOnly: false,
        Required: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    public static RadioRootState FromFieldState(
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

    internal Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>();

        if (Checked)
            attributes[RadioDataAttribute.Checked.ToDataAttributeString()] = string.Empty;
        else
            attributes[RadioDataAttribute.Unchecked.ToDataAttributeString()] = string.Empty;

        if (Disabled)
            attributes[RadioDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        if (ReadOnly)
            attributes[RadioDataAttribute.ReadOnly.ToDataAttributeString()] = string.Empty;

        if (Required)
            attributes[RadioDataAttribute.Required.ToDataAttributeString()] = string.Empty;

        if (Valid == true)
            attributes[RadioDataAttribute.Valid.ToDataAttributeString()] = string.Empty;
        else if (Valid == false)
            attributes[RadioDataAttribute.Invalid.ToDataAttributeString()] = string.Empty;

        if (Touched)
            attributes[RadioDataAttribute.Touched.ToDataAttributeString()] = string.Empty;

        if (Dirty)
            attributes[RadioDataAttribute.Dirty.ToDataAttributeString()] = string.Empty;

        if (Filled)
            attributes[RadioDataAttribute.Filled.ToDataAttributeString()] = string.Empty;

        if (Focused)
            attributes[RadioDataAttribute.Focused.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}
