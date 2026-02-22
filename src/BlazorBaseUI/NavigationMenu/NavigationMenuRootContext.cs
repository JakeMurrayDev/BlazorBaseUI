using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.NavigationMenu;

/// <summary>
/// Provides shared state and callbacks for the <see cref="NavigationMenuRoot"/> and its descendant components.
/// </summary>
internal sealed class NavigationMenuRootContext
{
    /// <summary>
    /// Gets the unique identifier for this navigation menu root instance.
    /// </summary>
    public string RootId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the value of the currently active item, or <see langword="null"/> if no item is active.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets whether the navigation menu popup is mounted in the DOM.
    /// </summary>
    public bool Mounted { get; set; }

    /// <summary>
    /// Gets whether this is a nested navigation menu.
    /// </summary>
    public bool IsNested { get; init; }

    /// <summary>
    /// Gets the visual orientation of the navigation menu.
    /// </summary>
    public NavigationMenuOrientation Orientation { get; init; }

    /// <summary>
    /// Gets or sets the direction from which the current item was activated.
    /// </summary>
    public ActivationDirection ActivationDirection { get; set; }

    /// <summary>
    /// Gets or sets the current transition animation status.
    /// </summary>
    public TransitionStatus TransitionStatus { get; set; }

    /// <summary>
    /// Gets or sets the type of instant transition to apply.
    /// </summary>
    public InstantType InstantType { get; set; }

    /// <summary>
    /// Gets the auto-generated ID of the popup element.
    /// Used by the trigger for <c>aria-controls</c> and by the popup for its <c>id</c>.
    /// </summary>
    public string PopupId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the delegate that returns the current active value.
    /// </summary>
    public Func<string?> GetValue { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that returns whether the menu is mounted.
    /// </summary>
    public Func<bool> GetMounted { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets the active value asynchronously.
    /// </summary>
    public Func<string?, Task> SetValueAsync { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets a trigger element reference for a specific item value.
    /// </summary>
    public Action<string, ElementReference?> SetTriggerElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that gets a trigger element reference for a specific item value.
    /// </summary>
    public Func<string, ElementReference?> GetTriggerElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets the popup element reference.
    /// </summary>
    public Action<ElementReference?> SetPopupElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets the viewport element reference.
    /// </summary>
    public Action<ElementReference?> SetViewportElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that registers an item value in the registration order.
    /// </summary>
    public Action<string> RegisterItem { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that unregisters an item value.
    /// </summary>
    public Action<string> UnregisterItem { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that sets a content element reference for a specific item value.
    /// </summary>
    public Action<string, ElementReference?> SetContentElement { get; init; } = null!;

    /// <summary>
    /// Gets the delegate that emits a close event.
    /// </summary>
    public Action EmitClose { get; init; } = null!;
}
