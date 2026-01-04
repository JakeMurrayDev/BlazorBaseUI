using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tabs;

public interface ITabsListContext
{
    bool ActivateOnFocus { get; }
    int HighlightedTabIndex { get; }
    ElementReference? TabsListElement { get; }
    void SetHighlightedTabIndex(int index);
    Task OnTabActivationAsync(object? value, ActivationDirection direction);
    Task<bool> NavigateToPreviousAsync(object currentTab);
    Task<bool> NavigateToNextAsync(object currentTab);
    Task NavigateToFirstAsync();
    Task NavigateToLastAsync();
}

public sealed class TabsListContext<TValue> : ITabsListContext
{
    private readonly ITabsRootContext<TValue> rootContext;

    public TabsListContext(
        bool activateOnFocus,
        int highlightedTabIndex,
        Action<int> setHighlightedTabIndex,
        Func<ElementReference?> getTabsListElement,
        ITabsRootContext<TValue> rootContext)
    {
        ActivateOnFocus = activateOnFocus;
        HighlightedTabIndex = highlightedTabIndex;
        SetHighlightedTabIndexInternal = setHighlightedTabIndex;
        GetTabsListElement = getTabsListElement;
        this.rootContext = rootContext;
    }

    public bool ActivateOnFocus { get; private set; }
    public int HighlightedTabIndex { get; private set; }
    public bool LoopFocus { get; private set; }
    private Action<int> SetHighlightedTabIndexInternal { get; }
    private Func<ElementReference?> GetTabsListElement { get; }

    public ElementReference? TabsListElement => GetTabsListElement();

    public void UpdateProperties(bool activateOnFocus, int highlightedTabIndex, bool loopFocus)
    {
        ActivateOnFocus = activateOnFocus;
        HighlightedTabIndex = highlightedTabIndex;
        LoopFocus = loopFocus;
    }

    public void SetHighlightedTabIndex(int index)
    {
        HighlightedTabIndex = index;
        SetHighlightedTabIndexInternal(index);
    }

    public async Task OnTabActivationAsync(object? value, ActivationDirection direction)
    {
        if (value is TValue typedValue)
        {
            await rootContext.OnValueChangeAsync(typedValue, direction);
        }
    }

    public async Task<bool> NavigateToPreviousAsync(object currentTab)
    {
        var ordered = rootContext.GetOrderedTabs();
        var currentIndex = FindTabIndex(ordered, currentTab);
        if (currentIndex < 0)
            return false;

        for (var i = currentIndex - 1; i >= 0; i--)
        {
            if (!ordered[i].IsDisabled())
            {
                SetHighlightedTabIndex(i);
                await ordered[i].Focus();
                return true;
            }
        }

        if (LoopFocus)
        {
            for (var i = ordered.Length - 1; i > currentIndex; i--)
            {
                if (!ordered[i].IsDisabled())
                {
                    SetHighlightedTabIndex(i);
                    await ordered[i].Focus();
                    return true;
                }
            }
        }

        return false;
    }

    public async Task<bool> NavigateToNextAsync(object currentTab)
    {
        var ordered = rootContext.GetOrderedTabs();
        var currentIndex = FindTabIndex(ordered, currentTab);
        if (currentIndex < 0)
            return false;

        for (var i = currentIndex + 1; i < ordered.Length; i++)
        {
            if (!ordered[i].IsDisabled())
            {
                SetHighlightedTabIndex(i);
                await ordered[i].Focus();
                return true;
            }
        }

        if (LoopFocus)
        {
            for (var i = 0; i < currentIndex; i++)
            {
                if (!ordered[i].IsDisabled())
                {
                    SetHighlightedTabIndex(i);
                    await ordered[i].Focus();
                    return true;
                }
            }
        }

        return false;
    }

    public async Task NavigateToFirstAsync()
    {
        var ordered = rootContext.GetOrderedTabs();
        for (var i = 0; i < ordered.Length; i++)
        {
            if (!ordered[i].IsDisabled())
            {
                SetHighlightedTabIndex(i);
                await ordered[i].Focus();
                return;
            }
        }
    }

    public async Task NavigateToLastAsync()
    {
        var ordered = rootContext.GetOrderedTabs();
        for (var i = ordered.Length - 1; i >= 0; i--)
        {
            if (!ordered[i].IsDisabled())
            {
                SetHighlightedTabIndex(i);
                await ordered[i].Focus();
                return;
            }
        }
    }

    private static int FindTabIndex(TabRegistration<TValue>[] ordered, object tab)
    {
        for (var i = 0; i < ordered.Length; i++)
        {
            if (ReferenceEquals(ordered[i].Tab, tab))
                return i;
        }
        return -1;
    }
}
