namespace BlazorBaseUI.Field;

internal static class Extensions
{
    extension(FieldDataAttribute attribute)
    {
        public string ToDataAttributeName() =>
            attribute switch
            {
                FieldDataAttribute.Disabled => "data-disabled",
                FieldDataAttribute.Valid => "data-valid",
                FieldDataAttribute.Invalid => "data-invalid",
                FieldDataAttribute.Touched => "data-touched",
                FieldDataAttribute.Dirty => "data-dirty",
                FieldDataAttribute.Filled => "data-filled",
                FieldDataAttribute.Focused => "data-focused",
                _ => throw new ArgumentOutOfRangeException(nameof(attribute), attribute, null)
            };
    }
}
