using System.ComponentModel;

namespace BlazorBaseUI.ToggleGroup;

internal static class Extensions
{
    extension(ToggleGroupDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                ToggleGroupDataAttribute.Disabled => "data-Disabled",
                ToggleGroupDataAttribute.Orientation => "data-Orientation",
                ToggleGroupDataAttribute.Multiple => "data-multiple",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(ToggleGroupDataAttribute))
            };
    }
}
