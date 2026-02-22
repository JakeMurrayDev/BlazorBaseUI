namespace BlazorBaseUI.NavigationMenu;

/// <summary>
/// Internal extension methods for the navigation menu component.
/// </summary>
internal static class Extensions
{
    extension(Side side)
    {
        /// <summary>
        /// Converts a <see cref="Side"/> value to its corresponding data attribute string.
        /// </summary>
        public string? ToDataAttributeString() => side switch
        {
            Side.Top => "top",
            Side.Bottom => "bottom",
            Side.Left => "left",
            Side.Right => "right",
            Side.InlineEnd => "inline-end",
            Side.InlineStart => "inline-start",
            _ => null
        };
    }

    extension(Align align)
    {
        /// <summary>
        /// Converts an <see cref="Align"/> value to its corresponding data attribute string.
        /// </summary>
        public string? ToDataAttributeString() => align switch
        {
            Align.Start => "start",
            Align.Center => "center",
            Align.End => "end",
            _ => null
        };
    }

    extension(CollisionBoundary collisionBoundary)
    {
        /// <summary>
        /// Converts a <see cref="CollisionBoundary"/> value to its corresponding data attribute string.
        /// </summary>
        public string ToDataAttributeString() => collisionBoundary switch
        {
            CollisionBoundary.Viewport => "viewport",
            CollisionBoundary.ClippingAncestors => "clipping-ancestors",
            _ => "clipping-ancestors"
        };
    }

    extension(CollisionAvoidance collisionAvoidance)
    {
        /// <summary>
        /// Converts a <see cref="CollisionAvoidance"/> value to its corresponding data attribute string.
        /// </summary>
        public string ToDataAttributeString() => collisionAvoidance switch
        {
            CollisionAvoidance.None => "none",
            CollisionAvoidance.Shift => "shift",
            CollisionAvoidance.Flip => "flip",
            CollisionAvoidance.FlipShift => "flip-shift",
            _ => "flip-shift"
        };
    }

    extension(ActivationDirection direction)
    {
        /// <summary>
        /// Converts an <see cref="ActivationDirection"/> value to its corresponding data attribute string.
        /// </summary>
        public string? ToDataAttributeString() => direction switch
        {
            ActivationDirection.Left => "left",
            ActivationDirection.Right => "right",
            ActivationDirection.Up => "up",
            ActivationDirection.Down => "down",
            _ => null
        };
    }

    extension(NavigationMenuOrientation orientation)
    {
        /// <summary>
        /// Converts a <see cref="NavigationMenuOrientation"/> value to its corresponding data attribute string.
        /// </summary>
        public string ToDataAttributeString() => orientation switch
        {
            NavigationMenuOrientation.Horizontal => "horizontal",
            NavigationMenuOrientation.Vertical => "vertical",
            _ => "horizontal"
        };
    }
}
