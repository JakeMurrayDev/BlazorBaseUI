namespace BlazorBaseUI.Collapsible;

public sealed record CollapsibleRootState(
    bool Open,
    bool Disabled,
    TransitionStatus TransitionStatus)
{
    public static CollapsibleRootState Default => new(false, false, TransitionStatus.Undefined);

    internal Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>();

        if (Open)
            attributes["data-panel-open"] = string.Empty;
        else
            attributes[CollapsibleDataAttribute.Closed.ToDataAttributeString()] = string.Empty;

        if (Disabled)
            attributes[CollapsibleDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}