namespace BlazorBaseUI.Collapsible;

public record CollapsibleRootState(
    bool Open,
    bool Disabled,
    TransitionStatus TransitionStatus)
{
    public static CollapsibleRootState Default => new(false, false, TransitionStatus.Undefined);

    public Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>();

        if (Open)
            attributes[CollapsibleDataAttribute.Open.ToDataAttributeString()] = string.Empty;
        else
            attributes[CollapsibleDataAttribute.Closed.ToDataAttributeString()] = string.Empty;

        if (Disabled)
            attributes[CollapsibleDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}