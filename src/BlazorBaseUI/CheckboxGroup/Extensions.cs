using System.ComponentModel;

namespace BlazorBaseUI.CheckboxGroup;

internal static class Extensions
{
    extension(CheckboxGroupDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                CheckboxGroupDataAttribute.Disabled => "data-Disabled",
                CheckboxGroupDataAttribute.Valid => "data-valid",
                CheckboxGroupDataAttribute.Invalid => "data-invalid",
                CheckboxGroupDataAttribute.Touched => "data-touched",
                CheckboxGroupDataAttribute.Dirty => "data-dirty",
                CheckboxGroupDataAttribute.Filled => "data-filled",
                CheckboxGroupDataAttribute.Focused => "data-focused",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(CheckboxGroupDataAttribute))
            };
    }
}