namespace BlazorBaseUI.Collapsible;

/// <summary>
/// Data attributes used by collapsible components.
/// </summary>
public enum CollapsibleDataAttribute
{
    /// <summary>
    /// Present when the collapsible is open.
    /// </summary>
    Open,

    /// <summary>
    /// Present when the collapsible is closed.
    /// </summary>
    Closed,

    /// <summary>
    /// Present when the collapsible is disabled.
    /// </summary>
    Disabled,

    /// <summary>
    /// Present on the trigger when the panel is open.
    /// </summary>
    PanelOpen,

    /// <summary>
    /// Present when the component is animating in (opening).
    /// </summary>
    StartingStyle,

    /// <summary>
    /// Present when the component is animating out (closing).
    /// </summary>
    EndingStyle
}

/// <summary>
/// Reasons for collapsible state changes.
/// Maps to React's REASONS constants.
/// </summary>
public enum CollapsibleChangeReason
{
    /// <summary>
    /// No specific reason (e.g., programmatic change, beforematch event).
    /// </summary>
    None,

    /// <summary>
    /// The trigger button was pressed.
    /// </summary>
    TriggerPress
}