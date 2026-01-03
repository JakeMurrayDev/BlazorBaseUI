using System.ComponentModel;

namespace BlazorBaseUI.Toggle;

internal static class Extensions
{
    extension(ToggleDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                ToggleDataAttribute.Pressed => "data-pressed",
                ToggleDataAttribute.Disabled => "data-Disabled",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(ToggleDataAttribute))
            };
    }
}
