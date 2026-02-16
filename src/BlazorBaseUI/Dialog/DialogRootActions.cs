namespace BlazorBaseUI.Dialog;

/// <summary>
/// Provides imperative actions for controlling a dialog programmatically.
/// </summary>
public sealed class DialogRootActions
{
    /// <summary>
    /// Gets the action that forces the dialog to unmount from the DOM.
    /// Useful when the dialog's animation is controlled by an external library.
    /// </summary>
    public Action? Unmount { get; internal set; }

    /// <summary>
    /// Gets the action that closes the dialog imperatively.
    /// </summary>
    public Action? Close { get; internal set; }

    /// <summary>
    /// Gets the action that opens the dialog imperatively.
    /// </summary>
    public Action? Open { get; internal set; }

    /// <summary>
    /// Gets the action that opens the dialog imperatively with a payload.
    /// </summary>
    public Action<object?>? OpenWithPayload { get; internal set; }
}
