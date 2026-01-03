using System.ComponentModel;

namespace BlazorBaseUI.Form;

internal static class Extensions
{
    extension(ValidationMode mode)
    {
        public string ToAttributeString() =>
            mode switch
            {
                ValidationMode.OnSubmit => "onSubmit",
                ValidationMode.OnBlur => "onBlur",
                ValidationMode.OnChange => "onChange",
                _ => throw new InvalidEnumArgumentException(nameof(mode), (int)mode, typeof(ValidationMode))
            };
    }
}
