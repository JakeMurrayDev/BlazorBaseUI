using System.ComponentModel;

namespace BlazorBaseUI.Collapsible;

internal static class Extensions
{
    extension(CollapsibleDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                CollapsibleDataAttribute.Open => "data-open",
                CollapsibleDataAttribute.Closed => "data-closed",
                CollapsibleDataAttribute.Disabled => "data-Disabled",
                CollapsibleDataAttribute.StartingStyle => "data-starting-style",
                CollapsibleDataAttribute.EndingStyle => "data-ending-style",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(CollapsibleDataAttribute))
            };
    }
}