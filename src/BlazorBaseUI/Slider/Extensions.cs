using System.ComponentModel;

namespace BlazorBaseUI.Slider;

internal static class Extensions
{
    extension(ThumbCollisionBehavior behavior)
    {
        public string ToAttributeString() =>
            behavior switch
            {
                ThumbCollisionBehavior.Push => "push",
                ThumbCollisionBehavior.Swap => "swap",
                ThumbCollisionBehavior.None => "none",
                _ => throw new InvalidEnumArgumentException(nameof(behavior), (int)behavior, typeof(ThumbCollisionBehavior))
            };

        public string ToDataAttributeString() =>
            behavior switch
            {
                ThumbCollisionBehavior.Push => "push",
                ThumbCollisionBehavior.Swap => "swap",
                ThumbCollisionBehavior.None => "none",
                _ => throw new InvalidEnumArgumentException(nameof(behavior), (int)behavior, typeof(ThumbCollisionBehavior))
            };
    }

    extension(ThumbAlignment alignment)
    {
        public string ToAttributeString() =>
            alignment switch
            {
                ThumbAlignment.Center => "center",
                ThumbAlignment.Edge => "edge",
                _ => throw new InvalidEnumArgumentException(nameof(alignment), (int)alignment, typeof(ThumbAlignment))
            };

        public string ToDataAttributeString() =>
            alignment switch
            {
                ThumbAlignment.Center => "center",
                ThumbAlignment.Edge => "edge",
                _ => throw new InvalidEnumArgumentException(nameof(alignment), (int)alignment, typeof(ThumbAlignment))
            };
    }

    extension(SliderDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                SliderDataAttribute.Dragging => "data-dragging",
                SliderDataAttribute.Orientation => "data-Orientation",
                SliderDataAttribute.Disabled => "data-Disabled",
                SliderDataAttribute.ReadOnly => "data-readonly",
                SliderDataAttribute.Required => "data-required",
                SliderDataAttribute.Valid => "data-valid",
                SliderDataAttribute.Invalid => "data-invalid",
                SliderDataAttribute.Touched => "data-touched",
                SliderDataAttribute.Dirty => "data-dirty",
                SliderDataAttribute.Focused => "data-focused",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(SliderDataAttribute))
            };
    }

    extension(SliderThumbDataAttribute attribute)
    {
        public string ToDataAttributeString() =>
            attribute switch
            {
                SliderThumbDataAttribute.Index => "data-index",
                SliderThumbDataAttribute.Dragging => "data-dragging",
                SliderThumbDataAttribute.Orientation => "data-Orientation",
                SliderThumbDataAttribute.Disabled => "data-Disabled",
                SliderThumbDataAttribute.ReadOnly => "data-readonly",
                SliderThumbDataAttribute.Required => "data-required",
                SliderThumbDataAttribute.Valid => "data-valid",
                SliderThumbDataAttribute.Invalid => "data-invalid",
                SliderThumbDataAttribute.Touched => "data-touched",
                SliderThumbDataAttribute.Dirty => "data-dirty",
                SliderThumbDataAttribute.Focused => "data-focused",
                _ => throw new InvalidEnumArgumentException(nameof(attribute), (int)attribute, typeof(SliderThumbDataAttribute))
            };
    }
}
