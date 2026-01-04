namespace BlazorBaseUI.Tabs;

public sealed record TabsRootState(Orientation Orientation, ActivationDirection ActivationDirection)
{
    public static TabsRootState Default { get; } = new(
        Orientation: Orientation.Horizontal,
        ActivationDirection: ActivationDirection.None);

    internal void WriteDataAttributes(Dictionary<string, object> attributes)
    {
        var orientationValue = Orientation.ToDataAttributeString();
        if (orientationValue is not null)
            attributes[TabsDataAttribute.Orientation.ToDataAttributeString()] = orientationValue;

        attributes[TabsDataAttribute.ActivationDirection.ToDataAttributeString()] = ActivationDirection.ToDataAttributeString();
    }
}
