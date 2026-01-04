namespace BlazorBaseUI.Tabs;

public sealed record TabsPanelState(bool Hidden, Orientation Orientation, ActivationDirection ActivationDirection)
{
    public static TabsPanelState Default { get; } = new(
        Hidden: true,
        Orientation: Orientation.Horizontal,
        ActivationDirection: ActivationDirection.None);

    internal void WriteDataAttributes(Dictionary<string, object> attributes)
    {
        var orientationValue = Orientation.ToDataAttributeString();
        if (orientationValue is not null)
            attributes[TabsDataAttribute.Orientation.ToDataAttributeString()] = orientationValue;

        attributes[TabsDataAttribute.ActivationDirection.ToDataAttributeString()] = ActivationDirection.ToDataAttributeString();

        if (Hidden)
            attributes[TabsDataAttribute.Hidden.ToDataAttributeString()] = string.Empty;
    }
}
