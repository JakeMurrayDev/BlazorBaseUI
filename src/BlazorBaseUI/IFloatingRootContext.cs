using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI;

/// <summary>
/// Shared interface for floating root contexts, enabling <see cref="FloatingTree.FloatingTree"/>
/// and <see cref="FloatingFocusManager.FloatingFocusManager"/> to work with any floating component generically.
/// </summary>
public interface IFloatingRootContext
{
    /// <summary>
    /// Gets the unique identifier of the floating element.
    /// </summary>
    string FloatingId { get; }

    /// <summary>
    /// Gets the current open state.
    /// </summary>
    bool GetOpen();

    /// <summary>
    /// Gets the trigger element reference, if any.
    /// </summary>
    ElementReference? GetTriggerElement();

    /// <summary>
    /// Gets the popup element reference, if any.
    /// </summary>
    ElementReference? GetPopupElement();

    /// <summary>
    /// Sets the popup element reference.
    /// </summary>
    void SetPopupElement(ElementReference element);

    /// <summary>
    /// Sets the open state asynchronously.
    /// </summary>
    Task SetOpenAsync(bool open);
}
