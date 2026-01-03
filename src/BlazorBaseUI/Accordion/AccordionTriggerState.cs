namespace BlazorBaseUI.Accordion;

public sealed record AccordionTriggerState(bool Open, Orientation Orientation, string Value, bool Disabled)
{
    internal Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>
        {
            [AccordionTriggerDataAttribute.Value.ToDataAttributeString()] = Value,
            [AccordionTriggerDataAttribute.Orientation.ToDataAttributeString()] = Orientation.ToDataAttributeString()!
        };

        if (Open)
            attributes[AccordionTriggerDataAttribute.PanelOpen.ToDataAttributeString()] = string.Empty;

        if (Disabled)
            attributes[AccordionTriggerDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}