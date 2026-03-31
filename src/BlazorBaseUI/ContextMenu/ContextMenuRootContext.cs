namespace BlazorBaseUI.ContextMenu;

using Microsoft.AspNetCore.Components;

/// <summary>
/// Provides shared state and callbacks for the <see cref="ContextMenuRoot"/> and its descendant components.
/// </summary>
internal sealed class ContextMenuRootContext
{
    /// <summary>
    /// Gets the unique identifier for this context menu root instance.
    /// </summary>
    public string RootId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the delegate that returns the virtual anchor element reference used for positioning.
    /// </summary>
    public Func<ElementReference?> GetVirtualAnchorElement { get; init; } = null!;

    /// <summary>
    /// Gets or sets the callback to register the backdrop element with the context menu JS,
    /// enabling native context menu suppression when right-clicking the backdrop.
    /// </summary>
    public Func<ElementReference, Task>? RegisterBackdropElement { get; set; }
}
