namespace BlazorBaseUI;

/// <summary>
/// Specifies the visual orientation of a component.
/// </summary>
public enum Orientation
{
    /// <summary>The orientation has not been set.</summary>
    Undefined,

    /// <summary>Horizontal orientation.</summary>
    Horizontal,

    /// <summary>Vertical orientation.</summary>
    Vertical
}

/// <summary>
/// Specifies the text reading direction.
/// </summary>
public enum Direction
{
    /// <summary>The direction has not been set.</summary>
    Undefined,

    /// <summary>Left-to-right.</summary>
    Ltr,

    /// <summary>Right-to-left.</summary>
    Rtl
}

/// <summary>
/// Describes the current phase of a CSS transition animation.
/// </summary>
public enum TransitionStatus
{
    /// <summary>No transition is active.</summary>
    Undefined,

    /// <summary>The element is animating in (opening).</summary>
    Starting,

    /// <summary>The element is animating out (closing).</summary>
    Ending,

    /// <summary>The transition has completed.</summary>
    Idle
}