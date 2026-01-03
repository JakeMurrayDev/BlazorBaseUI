using System.ComponentModel;

namespace BlazorBaseUI.Switch;

internal static class Extensions
{
    extension(SwitchDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                SwitchDataAttribute.Checked => "data-checked",
                SwitchDataAttribute.Unchecked => "data-unchecked",
                SwitchDataAttribute.Disabled => "data-Disabled",
                SwitchDataAttribute.ReadOnly => "data-readonly",
                SwitchDataAttribute.Required => "data-required",
                SwitchDataAttribute.Valid => "data-valid",
                SwitchDataAttribute.Invalid => "data-invalid",
                SwitchDataAttribute.Touched => "data-touched",
                SwitchDataAttribute.Dirty => "data-dirty",
                SwitchDataAttribute.Filled => "data-filled",
                SwitchDataAttribute.Focused => "data-focused",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(SwitchDataAttribute))
            };
    }
}