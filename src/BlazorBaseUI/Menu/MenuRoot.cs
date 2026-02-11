namespace BlazorBaseUI.Menu;

/// <summary>
/// Groups all parts of the menu.
/// Does not render an element.
/// </summary>
public partial class MenuRoot;

/// <summary>
/// Provides imperative actions for controlling the menu programmatically.
/// </summary>
public sealed class MenuRootActions
{
    /// <summary>
    /// Gets or sets the action to manually unmount the menu.
    /// </summary>
    public Action? Unmount { get; internal set; }

    /// <summary>
    /// Gets or sets the action to imperatively close the menu.
    /// </summary>
    public Action? Close { get; internal set; }
}
