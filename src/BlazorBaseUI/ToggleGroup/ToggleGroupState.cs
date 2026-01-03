namespace BlazorBaseUI.ToggleGroup;

public sealed record ToggleGroupState(bool Disabled, bool Multiple, Orientation Orientation)
{
    public static ToggleGroupState Default { get; } = new(
        Disabled: false,
        Multiple: false,
        Orientation: Orientation.Horizontal);

    internal Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>();

        if (Disabled)
            attributes[ToggleGroupDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        if (Multiple)
            attributes[ToggleGroupDataAttribute.Multiple.ToDataAttributeString()] = string.Empty;

        var orientationValue = Orientation.ToDataAttributeString();
        if (orientationValue is not null)
            attributes[ToggleGroupDataAttribute.Orientation.ToDataAttributeString()] = orientationValue;

        return attributes;
    }
}
