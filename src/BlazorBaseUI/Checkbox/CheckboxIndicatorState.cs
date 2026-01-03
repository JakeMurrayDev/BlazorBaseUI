namespace BlazorBaseUI.Checkbox;

public sealed record CheckboxIndicatorState(
    bool Checked,
    bool Disabled,
    bool ReadOnly,
    bool Required,
    bool Indeterminate,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused,
    TransitionStatus TransitionStatus)
{
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

        if (TransitionStatus == TransitionStatus.Starting)
            attributes[CheckboxDataAttribute.StartingStyle.ToDataAttributeString()] = string.Empty;
        else if (TransitionStatus == TransitionStatus.Ending)
            attributes[CheckboxDataAttribute.EndingStyle.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}