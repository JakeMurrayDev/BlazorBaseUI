using System.ComponentModel;

namespace BlazorBaseUI.Checkbox;

internal static class Extensions
{
    extension(CheckboxDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                CheckboxDataAttribute.Checked => "data-checked",
                CheckboxDataAttribute.Unchecked => "data-unchecked",
                CheckboxDataAttribute.Disabled => "data-disabled",
                CheckboxDataAttribute.ReadOnly => "data-readonly",
                CheckboxDataAttribute.Required => "data-required",
                CheckboxDataAttribute.Indeterminate => "data-indeterminate",
                CheckboxDataAttribute.Valid => "data-valid",
                CheckboxDataAttribute.Invalid => "data-invalid",
                CheckboxDataAttribute.Touched => "data-touched",
                CheckboxDataAttribute.Dirty => "data-dirty",
                CheckboxDataAttribute.Filled => "data-filled",
                CheckboxDataAttribute.Focused => "data-focused",
                CheckboxDataAttribute.StartingStyle => "data-starting-style",
                CheckboxDataAttribute.EndingStyle => "data-ending-style",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(CheckboxDataAttribute))
            };
    }
}