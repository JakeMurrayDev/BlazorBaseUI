using System.ComponentModel;

namespace BlazorBaseUI.RadioGroup;

internal static class Extensions
{
    extension(RadioGroupDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                RadioGroupDataAttribute.Disabled => "data-disabled",
                RadioGroupDataAttribute.ReadOnly => "data-readonly",
                RadioGroupDataAttribute.Required => "data-required",
                RadioGroupDataAttribute.Valid => "data-valid",
                RadioGroupDataAttribute.Invalid => "data-invalid",
                RadioGroupDataAttribute.Touched => "data-touched",
                RadioGroupDataAttribute.Dirty => "data-dirty",
                RadioGroupDataAttribute.Filled => "data-filled",
                RadioGroupDataAttribute.Focused => "data-focused",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(RadioGroupDataAttribute))
            };
    }
}
