using System.ComponentModel;

namespace BlazorBaseUI.Tabs;

internal static class Extensions
{
    extension(ActivationDirection direction)
    {
        public string ToDataAttributeString() =>
            direction switch
            {
                ActivationDirection.None => "none",
                ActivationDirection.Left => "left",
                ActivationDirection.Right => "right",
                ActivationDirection.Up => "up",
                ActivationDirection.Down => "down",
                _ => throw new InvalidEnumArgumentException(nameof(direction), (int)direction, typeof(ActivationDirection))
            };
    }

    extension(TabsDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                TabsDataAttribute.Orientation => "data-orientation",
                TabsDataAttribute.ActivationDirection => "data-activation-direction",
                TabsDataAttribute.Disabled => "data-disabled",
                TabsDataAttribute.Active => "data-active",
                TabsDataAttribute.Hidden => "data-hidden",
                TabsDataAttribute.Index => "data-index",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(TabsDataAttribute))
            };
    }
}
