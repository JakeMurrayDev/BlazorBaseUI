namespace BlazorBaseUI.Accordion;

public sealed record AccordionPanelState(
    bool Open,
    bool Disabled,
    int Index,
    Orientation Orientation,
    TransitionStatus TransitionStatus)
{
    internal Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>
        {
            [AccordionPanelDataAttribute.Index.ToDataAttributeString()] = Index.ToString(),
            [AccordionPanelDataAttribute.Orientation.ToDataAttributeString()] = Orientation.ToDataAttributeString()!
        };

        if (Open)
            attributes[AccordionPanelDataAttribute.Open.ToDataAttributeString()] = string.Empty;

        if (Disabled)
            attributes[AccordionPanelDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}