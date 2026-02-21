using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tabs;

/// <summary>
/// Provides context for the tabs list, enabling child tab components
/// to access list-level settings and trigger tab activation.
/// </summary>
internal interface ITabsListContext
{
    /// <summary>
    /// Gets a value indicating whether tabs are activated automatically on focus.
    /// </summary>
    bool ActivateOnFocus { get; }

    /// <summary>
    /// Gets a value indicating whether keyboard focus loops back to the first tab
    /// when the end of the list is reached.
    /// </summary>
    bool LoopFocus { get; }

    /// <summary>
    /// Gets the <see cref="ElementReference"/> of the tabs list container.
    /// </summary>
    ElementReference? TabsListElement { get; }

    /// <summary>
    /// Activates the tab with the specified value and direction.
    /// </summary>
    /// <param name="value">The tab value to activate.</param>
    /// <param name="direction">The direction of activation.</param>
    Task OnTabActivationAsync(object? value, ActivationDirection direction);
}

/// <summary>
/// Default implementation of <see cref="ITabsListContext"/> that delegates
/// tab activation to the root context.
/// </summary>
/// <typeparam name="TValue">The type of value used to identify tabs.</typeparam>
internal sealed class TabsListContext<TValue> : ITabsListContext
{
    private readonly ITabsRootContext<TValue> rootContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="TabsListContext{TValue}"/> class.
    /// </summary>
    /// <param name="activateOnFocus">Whether to activate tabs on focus.</param>
    /// <param name="loopFocus">Whether to loop keyboard focus.</param>
    /// <param name="getTabsListElement">A function that returns the list element reference.</param>
    /// <param name="rootContext">The root context to delegate activation to.</param>
    public TabsListContext(
        bool activateOnFocus,
        bool loopFocus,
        Func<ElementReference?> getTabsListElement,
        ITabsRootContext<TValue> rootContext)
    {
        ActivateOnFocus = activateOnFocus;
        LoopFocus = loopFocus;
        GetTabsListElement = getTabsListElement;
        this.rootContext = rootContext;
    }

    /// <inheritdoc />
    public bool ActivateOnFocus { get; set; }

    /// <inheritdoc />
    public bool LoopFocus { get; set; }

    private Func<ElementReference?> GetTabsListElement { get; }

    /// <inheritdoc />
    public ElementReference? TabsListElement => GetTabsListElement();

    /// <inheritdoc />
    public async Task OnTabActivationAsync(object? value, ActivationDirection direction)
    {
        if (value is TValue typedValue)
        {
            await rootContext.OnValueChangeAsync(typedValue, direction);
        }
    }
}
