namespace BlazorBaseUI.Tabs;

/// <summary>
/// Specifies the direction of a tab activation relative to the previously active tab.
/// </summary>
public enum ActivationDirection
{
    /// <summary>
    /// No direction (initial load or programmatic change).
    /// </summary>
    None,

    /// <summary>
    /// The new tab is to the left of the previous tab.
    /// </summary>
    Left,

    /// <summary>
    /// The new tab is to the right of the previous tab.
    /// </summary>
    Right,

    /// <summary>
    /// The new tab is above the previous tab.
    /// </summary>
    Up,

    /// <summary>
    /// The new tab is below the previous tab.
    /// </summary>
    Down
}
