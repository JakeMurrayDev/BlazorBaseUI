namespace BlazorBaseUI.Tabs;

public sealed record TabsIndicatorState(
    Orientation Orientation,
    ActivationDirection ActivationDirection,
    TabPosition? ActiveTabPosition,
    TabSize? ActiveTabSize)
{
    public static TabsIndicatorState Default { get; } = new(
        Orientation: Orientation.Horizontal,
        ActivationDirection: ActivationDirection.None,
        ActiveTabPosition: null,
        ActiveTabSize: null);

    internal void WriteDataAttributes(Dictionary<string, object> attributes)
    {
        var orientationValue = Orientation.ToDataAttributeString();
        if (orientationValue is not null)
            attributes[TabsDataAttribute.Orientation.ToDataAttributeString()] = orientationValue;

        attributes[TabsDataAttribute.ActivationDirection.ToDataAttributeString()] = ActivationDirection.ToDataAttributeString();
    }
}
