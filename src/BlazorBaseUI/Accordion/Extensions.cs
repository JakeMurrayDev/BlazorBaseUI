using System.ComponentModel;

namespace BlazorBaseUI.Accordion;

internal static class Extensions
{
    extension(AccordionRootDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                AccordionRootDataAttribute.Disabled => "data-disabled",
                AccordionRootDataAttribute.Orientation => "data-orientation",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(AccordionRootDataAttribute))
            };
    }

    extension(AccordionItemDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                AccordionItemDataAttribute.Index => "data-index",
                AccordionItemDataAttribute.Orientation => "data-orientation",
                AccordionItemDataAttribute.Disabled => "data-disabled",
                AccordionItemDataAttribute.Open => "data-open",
                AccordionItemDataAttribute.Closed => "data-closed",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(AccordionItemDataAttribute))
            };
    }

    extension(AccordionHeaderDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                AccordionHeaderDataAttribute.Index => "data-index",
                AccordionHeaderDataAttribute.Orientation => "data-orientation",
                AccordionHeaderDataAttribute.Disabled => "data-disabled",
                AccordionHeaderDataAttribute.Open => "data-open",
                AccordionHeaderDataAttribute.Closed => "data-closed",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(AccordionHeaderDataAttribute))
            };
    }

    extension(AccordionTriggerDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                AccordionTriggerDataAttribute.Value => "data-value",
                AccordionTriggerDataAttribute.PanelOpen => "data-panel-open",
                AccordionTriggerDataAttribute.Orientation => "data-orientation",
                AccordionTriggerDataAttribute.Disabled => "data-disabled",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(AccordionTriggerDataAttribute))
            };
    }

    extension(AccordionPanelDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                AccordionPanelDataAttribute.Index => "data-index",
                AccordionPanelDataAttribute.Open => "data-open",
                AccordionPanelDataAttribute.Orientation => "data-orientation",
                AccordionPanelDataAttribute.Disabled => "data-disabled",
                AccordionPanelDataAttribute.StartingStyle => "data-starting-style",
                AccordionPanelDataAttribute.EndingStyle => "data-ending-style",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(AccordionPanelDataAttribute))
            };
    }
}