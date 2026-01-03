namespace BlazorBaseUI.Toggle;

public sealed record ToggleState(bool Pressed, bool Disabled)
{
    public static ToggleState Default { get; } = new(Pressed: false, Disabled: false);

    internal Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>();

        if (Pressed)
            attributes[ToggleDataAttribute.Pressed.ToDataAttributeString()] = string.Empty;

        if (Disabled)
            attributes[ToggleDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}
