using BlazorBaseUI.Collapsible;

namespace BlazorBaseUI.Accordion;

public record AccordionPanelState<TValue>(
    TValue[] Value,
    bool Disabled,
    Orientation Orientation,
    int Index,
    bool Open,
    TransitionStatus TransitionStatus) : AccordionItemState<TValue>(Value, Disabled, Orientation, Index, Open)
{
    public new Dictionary<string, object> GetDataAttributes()
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