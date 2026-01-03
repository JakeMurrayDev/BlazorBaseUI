using BlazorBaseUI.Field;

namespace BlazorBaseUI.Checkbox;

public sealed record CheckboxRootState(
    bool Checked,
    bool Disabled,
    bool ReadOnly,
    bool Required,
    bool Indeterminate,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    public static CheckboxRootState Default { get; } = new(
        Checked: false,
        Disabled: false,
        ReadOnly: false,
        Required: false,
        Indeterminate: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    public static CheckboxRootState FromFieldState(
        FieldRootState fieldState,
        bool isChecked,
        bool isDisabled,
        bool isReadOnly,
        bool isRequired,
        bool isIndeterminate) => new(
            Checked: isChecked,
            Disabled: isDisabled,
            ReadOnly: isReadOnly,
            Required: isRequired,
            Indeterminate: isIndeterminate,
            Valid: fieldState.Valid,
            Touched: fieldState.Touched,
            Dirty: fieldState.Dirty,
            Filled: fieldState.Filled,
            Focused: fieldState.Focused);

    internal Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>();

        if (Indeterminate)
        {
            attributes[CheckboxDataAttribute.Indeterminate.ToDataAttributeString()] = string.Empty;
        }
        else if (Checked)
        {
            attributes[CheckboxDataAttribute.Checked.ToDataAttributeString()] = string.Empty;
        }
        else
        {
            attributes[CheckboxDataAttribute.Unchecked.ToDataAttributeString()] = string.Empty;
        }

        if (Disabled)
            attributes[CheckboxDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        if (ReadOnly)
            attributes[CheckboxDataAttribute.ReadOnly.ToDataAttributeString()] = string.Empty;

        if (Required)
            attributes[CheckboxDataAttribute.Required.ToDataAttributeString()] = string.Empty;

        if (Valid == true)
            attributes[CheckboxDataAttribute.Valid.ToDataAttributeString()] = string.Empty;
        else if (Valid == false)
            attributes[CheckboxDataAttribute.Invalid.ToDataAttributeString()] = string.Empty;

        if (Touched)
            attributes[CheckboxDataAttribute.Touched.ToDataAttributeString()] = string.Empty;

        if (Dirty)
            attributes[CheckboxDataAttribute.Dirty.ToDataAttributeString()] = string.Empty;

        if (Filled)
            attributes[CheckboxDataAttribute.Filled.ToDataAttributeString()] = string.Empty;

        if (Focused)
            attributes[CheckboxDataAttribute.Focused.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}