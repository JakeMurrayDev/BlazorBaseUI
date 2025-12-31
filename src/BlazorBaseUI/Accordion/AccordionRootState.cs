namespace BlazorBaseUI.Accordion;

public record AccordionRootState<TValue>(
    TValue[] Value,
    bool Disabled,
    Orientation Orientation)
{
    public Dictionary<string, object> GetDataAttributes()
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