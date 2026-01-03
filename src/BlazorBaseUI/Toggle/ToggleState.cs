namespace BlazorBaseUI.Toggle;

public record ToggleState(bool Pressed, bool Disabled)
{
    public static ToggleState Default { get; } = new(Pressed: false, Disabled: false);

    public Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>();

        if (Pressed)
            attributes[ToggleDataAttribute.Pressed.ToDataAttributeString()] = string.Empty;

        if (Disabled)
            attributes[ToggleDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}
