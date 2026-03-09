using System.ComponentModel;
using System.Globalization;


namespace BlazorBaseUI;

/// <summary>
/// Provides extension methods for converting common enumerations and types to their string representations.
/// </summary>
internal static class Extensions
{
    extension (Guid value)
    {
        public string ToIdString() =>
            value.ToString("N", CultureInfo.InvariantCulture)[..8];
    }

    extension(Orientation orientation)
    {
        public string? ToDataAttributeString() =>
            orientation switch
            {
                Orientation.Undefined => null,
                Orientation.Horizontal => "horizontal",
                Orientation.Vertical => "vertical",
                _ => throw new InvalidEnumArgumentException(nameof(orientation), (int)orientation, typeof(Orientation))
            };
    }

    extension(Direction direction)
    {
        public string? ToAttributeString() =>
            direction switch
            {
                Direction.Undefined => null,
                Direction.Ltr => "ltr",
                Direction.Rtl => "rtl",
                _ => throw new InvalidEnumArgumentException(nameof(direction), (int)direction, typeof(Direction))
            };
    }

    extension(TransitionStatus status)
    {
        public string? ToDataAttributeString() =>
            status switch
            {
                TransitionStatus.Undefined => null,
                TransitionStatus.Starting => "starting",
                TransitionStatus.Ending => "ending",
                TransitionStatus.Idle => "idle",
                _ => throw new InvalidEnumArgumentException(nameof(status), (int)status, typeof(TransitionStatus))
            };
    }

    extension(Side side)
    {
        public string? ToDataAttributeString() =>
            side switch
            {
                Side.Top => "top",
                Side.Bottom => "bottom",
                Side.Left => "left",
                Side.Right => "right",
                Side.InlineEnd => "inline-end",
                Side.InlineStart => "inline-start",
                _ => throw new InvalidEnumArgumentException(nameof(side), (int)side, typeof(Side))
            };
    }

    extension(Align align)
    {
        public string? ToDataAttributeString() =>
            align switch
            {
                Align.Start => "start",
                Align.Center => "center",
                Align.End => "end",
                _ => throw new InvalidEnumArgumentException(nameof(align), (int)align, typeof(Align))
            };
    }

    extension(CollisionBoundary collisionBoundary)
    {
        public string ToDataAttributeString() =>
            collisionBoundary switch
            {
                CollisionBoundary.Viewport => "viewport",
                CollisionBoundary.ClippingAncestors => "clipping-ancestors",
                _ => throw new InvalidEnumArgumentException(nameof(collisionBoundary), (int)collisionBoundary, typeof(CollisionBoundary))
            };
    }

    extension(CollisionAvoidance collisionAvoidance)
    {
        public string ToDataAttributeString() =>
            collisionAvoidance switch
            {
                CollisionAvoidance.None => "none",
                CollisionAvoidance.Shift => "shift",
                CollisionAvoidance.Flip => "flip",
                CollisionAvoidance.FlipShift => "flip-shift",
                _ => throw new InvalidEnumArgumentException(nameof(collisionAvoidance), (int)collisionAvoidance, typeof(CollisionAvoidance))
            };
    }

    extension(ActivationDirection direction)
    {
        public string ToDataAttributeString() =>
            direction switch
            {
                ActivationDirection.None => "none",
                ActivationDirection.Left => "left",
                ActivationDirection.Right => "right",
                ActivationDirection.Up => "up",
                ActivationDirection.Down => "down",
                _ => throw new InvalidEnumArgumentException(nameof(direction), (int)direction, typeof(ActivationDirection))
            };
    }
}
