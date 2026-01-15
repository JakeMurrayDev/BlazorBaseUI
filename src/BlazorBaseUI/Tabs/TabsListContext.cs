using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tabs;

public interface ITabsListContext
{
    bool ActivateOnFocus { get; }
    bool LoopFocus { get; }
    ElementReference? TabsListElement { get; }
    Task OnTabActivationAsync(object? value, ActivationDirection direction);
}

public sealed class TabsListContext<TValue> : ITabsListContext
{
    private readonly ITabsRootContext<TValue> rootContext;

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

    public bool ActivateOnFocus { get; set; }
    public bool LoopFocus { get; set; }
    private Func<ElementReference?> GetTabsListElement { get; }

    public ElementReference? TabsListElement => GetTabsListElement();

    public async Task OnTabActivationAsync(object? value, ActivationDirection direction)
    {
        if (value is TValue typedValue)
        {
            await rootContext.OnValueChangeAsync(typedValue, direction);
        }
    }
}
