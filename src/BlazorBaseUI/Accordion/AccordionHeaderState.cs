namespace BlazorBaseUI.Accordion;

public sealed record AccordionHeaderState(int Index, Orientation Orientation, bool Disabled, bool Open)
{
    internal Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>
        {
            [AccordionHeaderDataAttribute.Index.ToDataAttributeString()] = Index.ToString(),
            [AccordionHeaderDataAttribute.Orientation.ToDataAttributeString()] = Orientation.ToDataAttributeString()!
        };

        if (Open)
            attributes[AccordionHeaderDataAttribute.Open.ToDataAttributeString()] = string.Empty;
        else
            attributes[AccordionHeaderDataAttribute.Closed.ToDataAttributeString()] = string.Empty;

        if (Disabled)
            attributes[AccordionHeaderDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}