namespace BlazorBaseUI.Accordion;

public sealed record AccordionRootState<TValue>(
    TValue[] Value,
    bool Disabled,
    Orientation Orientation)
{
    internal Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>
        {
            [AccordionRootDataAttribute.Orientation.ToDataAttributeString()] = Orientation.ToDataAttributeString()!
        };

        if (Disabled)
            attributes[AccordionRootDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}