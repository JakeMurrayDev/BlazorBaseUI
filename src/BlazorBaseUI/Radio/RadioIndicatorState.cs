namespace BlazorBaseUI.Radio;

public record RadioIndicatorState(
    bool Checked,
    bool Disabled,
    bool ReadOnly,
    bool Required,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused,
    TransitionStatus TransitionStatus)
{
    public Dictionary<string, object> GetDataAttributes()
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

        if (TransitionStatus == TransitionStatus.Starting)
            attributes[RadioDataAttribute.StartingStyle.ToDataAttributeString()] = string.Empty;
        else if (TransitionStatus == TransitionStatus.Ending)
            attributes[RadioDataAttribute.EndingStyle.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}
