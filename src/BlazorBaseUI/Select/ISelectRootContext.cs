using BlazorBaseUI.Field;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Select;

/// <summary>
/// Provides non-generic context for the select root, enabling cross-component
/// communication between the root and its descendant components.
/// </summary>
internal interface ISelectRootContext
{
    /// <summary>
    /// Gets the unique identifier for this select root instance.
    /// </summary>
    string RootId { get; }

    /// <summary>
    /// Gets or sets whether the select popup is open.
    /// </summary>
    bool Open { get; set; }

    /// <summary>
    /// Gets or sets whether the select popup is mounted in the DOM.
    /// </summary>
    bool Mounted { get; set; }

    /// <summary>
    /// Gets or sets whether the select is disabled.
    /// </summary>
    bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets whether the select is read-only.
    /// </summary>
    bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets whether the select is required.
    /// </summary>
    bool Required { get; set; }

    /// <summary>
    /// Gets or sets whether the select supports multiple selection.
    /// </summary>
    bool Multiple { get; set; }

    /// <summary>
    /// Gets or sets whether items should be highlighted when the pointer moves over them.
    /// </summary>
    bool HighlightItemOnHover { get; set; }

    /// <summary>
    /// Gets or sets the reason the select's open state last changed.
    /// </summary>
    SelectOpenChangeReason OpenChangeReason { get; set; }

    /// <summary>
    /// Gets or sets the current transition animation status.
    /// </summary>
    TransitionStatus TransitionStatus { get; set; }

    /// <summary>
    /// Gets or sets the index of the currently highlighted item.
    /// </summary>
    int ActiveIndex { get; set; }

    /// <summary>
    /// Gets or sets the ID of the list element (when SelectList is used).
    /// </summary>
    string? ListId { get; set; }

    /// <summary>
    /// Gets whether a SelectList is present.
    /// </summary>
    bool HasList { get; }

    /// <summary>
    /// Gets or sets whether the scroll-up arrow should be visible.
    /// </summary>
    bool ScrollUpArrowVisible { get; set; }

    /// <summary>
    /// Gets or sets whether the scroll-down arrow should be visible.
    /// </summary>
    bool ScrollDownArrowVisible { get; set; }

    /// <summary>
    /// Gets the delegate that returns the current open state.
    /// </summary>
    Func<bool> GetOpen { get; }

    /// <summary>
    /// Gets the delegate that returns whether the select popup is mounted.
    /// </summary>
    Func<bool> GetMounted { get; }

    /// <summary>
    /// Gets the delegate that returns the trigger element reference.
    /// </summary>
    Func<ElementReference?> GetTriggerElement { get; }

    /// <summary>
    /// Gets the delegate that sets the trigger element reference.
    /// </summary>
    Action<ElementReference?> SetTriggerElement { get; }

    /// <summary>
    /// Gets the delegate that sets the popup element reference.
    /// </summary>
    Action<ElementReference?> SetPopupElement { get; }

    /// <summary>
    /// Gets the delegate that sets the list element reference.
    /// </summary>
    Action<ElementReference?> SetListElement { get; }

    /// <summary>
    /// Gets the delegate that sets the active item index.
    /// </summary>
    Action<int> SetActiveIndex { get; }

    /// <summary>
    /// Gets the delegate that sets the open state asynchronously with a reason.
    /// </summary>
    Func<bool, SelectOpenChangeReason, Task> SetOpenAsync { get; }

    /// <summary>
    /// Gets or sets the ID of the popup element (when no SelectList is present).
    /// </summary>
    string? PopupId { get; set; }

    /// <summary>
    /// Gets the delegate that clears keyboard-set highlights from the DOM.
    /// </summary>
    Func<Task> ClearHighlightsAsync { get; }

    /// <summary>
    /// Gets whether the current value represents a placeholder state (no selection).
    /// </summary>
    bool IsPlaceholder { get; }

    /// <summary>
    /// Checks whether a given value (boxed) is currently selected.
    /// </summary>
    bool IsValueSelected(object? value);

    /// <summary>
    /// Raised when the select's state changes (value, open, etc.),
    /// allowing child components to re-render.
    /// </summary>
    event Action? StateChanged;

    /// <summary>
    /// Raised when keyboard navigation takes over highlighting,
    /// signaling items to clear their mouse-set highlights.
    /// </summary>
    event Action? HighlightCleared;

    /// <summary>
    /// Gets the current field state from the parent FieldRoot, if any.
    /// </summary>
    FieldRootState FieldState { get; }

    /// <summary>
    /// Notifies the root that the trigger has received focus.
    /// </summary>
    void OnTriggerFocus();

    /// <summary>
    /// Notifies the root that the trigger has lost focus.
    /// </summary>
    void OnTriggerBlur();
}
