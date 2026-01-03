using System.ComponentModel;

namespace BlazorBaseUI.Radio;

internal static class Extensions
{
    extension(RadioDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                RadioDataAttribute.Checked => "data-checked",
                RadioDataAttribute.Unchecked => "data-unchecked",
                RadioDataAttribute.Disabled => "data-disabled",
                RadioDataAttribute.ReadOnly => "data-readonly",
                RadioDataAttribute.Required => "data-required",
                RadioDataAttribute.Valid => "data-valid",
                RadioDataAttribute.Invalid => "data-invalid",
                RadioDataAttribute.Touched => "data-touched",
                RadioDataAttribute.Dirty => "data-dirty",
                RadioDataAttribute.Filled => "data-filled",
                RadioDataAttribute.Focused => "data-focused",
                RadioDataAttribute.StartingStyle => "data-starting-style",
                RadioDataAttribute.EndingStyle => "data-ending-style",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(RadioDataAttribute))
            };
    }
}
