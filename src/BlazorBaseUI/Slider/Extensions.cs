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
}
