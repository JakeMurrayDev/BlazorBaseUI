namespace BlazorBaseUI.Tabs;

public sealed record TabsTabState(bool Active, bool Disabled, Orientation Orientation)
{
    public static TabsTabState Default { get; } = new(
        Active: false,
        Disabled: false,
        Orientation: Orientation.Horizontal);

    internal void WriteDataAttributes(Dictionary<string, object> attributes)
    {
        var orientationValue = Orientation.ToDataAttributeString();
        if (orientationValue is not null)
            attributes[TabsDataAttribute.Orientation.ToDataAttributeString()] = orientationValue;

        if (Active)
            attributes[TabsDataAttribute.Active.ToDataAttributeString()] = string.Empty;

        if (Disabled)
            attributes[TabsDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;
    }
}
