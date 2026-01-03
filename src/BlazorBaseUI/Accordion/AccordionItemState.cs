namespace BlazorBaseUI.Accordion;

public sealed record AccordionItemState<TValue>(
    TValue[] Value,
    bool Disabled,
    Orientation Orientation,
    int Index,
    bool Open)
{
    public Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>
        {
            [AccordionItemDataAttribute.Index.ToDataAttributeString()] = Index.ToString(),
            [AccordionItemDataAttribute.Orientation.ToDataAttributeString()] = Orientation.ToDataAttributeString()!
        };

        if (Open)
            attributes[AccordionItemDataAttribute.Open.ToDataAttributeString()] = string.Empty;
        else
            attributes[AccordionItemDataAttribute.Closed.ToDataAttributeString()] = string.Empty;

        if (Disabled)
            attributes[AccordionItemDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}