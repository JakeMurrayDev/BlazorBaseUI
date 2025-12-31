namespace BlazorBaseUI.Accordion;

public enum AccordionRootDataAttribute
{
    Disabled,
    Orientation
}

public enum AccordionItemDataAttribute
{
    Index,
    Orientation,
    Disabled,
    Open,
    Closed
}

public enum AccordionHeaderDataAttribute
{
    Index,
    Orientation,
    Disabled,
    Open,
    Closed
}

public enum AccordionTriggerDataAttribute
{
    Value,
    PanelOpen,
    Orientation,
    Disabled
}

public enum AccordionPanelDataAttribute
{
    Index,
    Open,
    Orientation,
    Disabled,
    StartingStyle,
    EndingStyle
}