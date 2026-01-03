using System.ComponentModel;

namespace BlazorBaseUI.Accordion;

internal static class Extensions
{
    extension(AccordionRootDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                AccordionRootDataAttribute.Disabled => "data-Disabled",
                AccordionRootDataAttribute.Orientation => "data-Orientation",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(AccordionRootDataAttribute))
            };
    }

    extension(AccordionItemDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                AccordionItemDataAttribute.Index => "data-index",
                AccordionItemDataAttribute.Orientation => "data-Orientation",
                AccordionItemDataAttribute.Disabled => "data-Disabled",
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
                AccordionHeaderDataAttribute.Orientation => "data-Orientation",
                AccordionHeaderDataAttribute.Disabled => "data-Disabled",
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
                AccordionTriggerDataAttribute.Value => "data-Value",
                AccordionTriggerDataAttribute.PanelOpen => "data-panel-open",
                AccordionTriggerDataAttribute.Orientation => "data-Orientation",
                AccordionTriggerDataAttribute.Disabled => "data-Disabled",
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
                AccordionPanelDataAttribute.Orientation => "data-Orientation",
                AccordionPanelDataAttribute.Disabled => "data-Disabled",
                AccordionPanelDataAttribute.StartingStyle => "data-starting-style",
                AccordionPanelDataAttribute.EndingStyle => "data-ending-style",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(AccordionPanelDataAttribute))
            };
    }
}