using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tabs;

public interface ITabsRootContext
{
    Orientation Orientation { get; }
    ActivationDirection ActivationDirection { get; }
    string? GetTabPanelIdByValue(object? tabValue);
    string? GetTabIdByPanelValue(object? panelValue);
    ElementReference? GetTabElementByValue(object? value);
    void RegisterPanel(object? panelValue, string panelId);
    void UnregisterPanel(object? panelValue, string panelId);
}

public interface ITabsRootContext<TValue> : ITabsRootContext
{
    TValue? Value { get; }
    Task OnValueChangeAsync(TValue? value, ActivationDirection direction);
    void RegisterTab(object tab, ElementReference element, TValue? value, string? id, Func<bool> isDisabled, Func<ValueTask> focus);
    void UnregisterTab(object tab);
    TabRegistration<TValue>? GetFirstEnabledTab();
}

public sealed class TabRegistration<TValue>(
    object Tab,
    ElementReference Element,
    TValue? Value,
    string? Id,
    Func<bool> IsDisabled,
    Func<ValueTask> Focus)
{
    public object Tab { get; } = Tab;
    public ElementReference Element { get; set; } = Element;
    public TValue? Value { get; } = Value;
    public string? Id { get; set; } = Id;
    public Func<bool> IsDisabled { get; } = IsDisabled;
    public Func<ValueTask> Focus { get; } = Focus;
}

public sealed class TabsRootContext<TValue> : ITabsRootContext<TValue>
{
    private readonly Dictionary<object, TabRegistration<TValue>> tabsByInstance = [];
    private readonly List<object> tabRegistrationOrder = [];
    private readonly Dictionary<object, string> panelIdsByValue = [];
    private TabRegistration<TValue>[]? orderedTabsCache;

    public TabsRootContext(
        Orientation orientation,
        ActivationDirection activationDirection,
        Func<TValue?> getValue,
        Func<TValue?, ActivationDirection, Task> onValueChange)
    {
        Orientation = orientation;
        ActivationDirection = activationDirection;
        GetValue = getValue;
        OnValueChange = onValueChange;
    }

    public Orientation Orientation { get; private set; }
    public ActivationDirection ActivationDirection { get; private set; }
    private Func<TValue?> GetValue { get; }
    private Func<TValue?, ActivationDirection, Task> OnValueChange { get; }

    public TValue? Value => GetValue();

    public void UpdateProperties(Orientation orientation, ActivationDirection activationDirection)
    {
        Orientation = orientation;
        ActivationDirection = activationDirection;
    }

    public async Task OnValueChangeAsync(TValue? value, ActivationDirection direction)
    {
        await OnValueChange(value, direction);
    }

    public void RegisterTab(object tab, ElementReference element, TValue? value, string? id, Func<bool> isDisabled, Func<ValueTask> focus)
    {
        if (tabsByInstance.TryGetValue(tab, out var existing))
        {
            existing.Element = element;
            existing.Id = id;
        }
        else
        {
            tabsByInstance[tab] = new TabRegistration<TValue>(tab, element, value, id, isDisabled, focus);
            tabRegistrationOrder.Add(tab);
        }
        orderedTabsCache = null;
    }

    public void UnregisterTab(object tab)
    {
        if (tabsByInstance.Remove(tab))
        {
            tabRegistrationOrder.Remove(tab);
            orderedTabsCache = null;
        }
    }

    public void RegisterPanel(object? panelValue, string panelId)
    {
        if (panelValue is null)
            return;

        panelIdsByValue[panelValue] = panelId;
    }

    public void UnregisterPanel(object? panelValue, string panelId)
    {
        if (panelValue is null)
            return;

        if (panelIdsByValue.TryGetValue(panelValue, out var existingId) && existingId == panelId)
        {
            panelIdsByValue.Remove(panelValue);
        }
    }

    public string? GetTabPanelIdByValue(object? tabValue)
    {
        if (tabValue is null)
            return null;

        return panelIdsByValue.TryGetValue(tabValue, out var panelId) ? panelId : null;
    }

    public string? GetTabIdByPanelValue(object? panelValue)
    {
        if (panelValue is null)
            return null;

        var ordered = GetOrderedTabs();
        for (var i = 0; i < ordered.Length; i++)
        {
            var tab = ordered[i];
            if (tab.Value is not null && tab.Value.Equals(panelValue))
            {
                return tab.Id;
            }
        }
        return null;
    }

    public ElementReference? GetTabElementByValue(object? value)
    {
        if (value is null)
            return null;

        var ordered = GetOrderedTabs();
        for (var i = 0; i < ordered.Length; i++)
        {
            var tab = ordered[i];
            if (tab.Value is not null && tab.Value.Equals(value))
            {
                return tab.Element;
            }
        }
        return null;
    }

    public TabRegistration<TValue>? GetFirstEnabledTab()
    {
        var ordered = GetOrderedTabs();
        for (var i = 0; i < ordered.Length; i++)
        {
            var tab = ordered[i];
            if (!tab.IsDisabled())
            {
                return tab;
            }
        }
        return null;
    }

    public TabRegistration<TValue>[] GetOrderedTabs()
    {
        if (orderedTabsCache is not null)
            return orderedTabsCache;

        var result = new TabRegistration<TValue>[tabRegistrationOrder.Count];
        for (var i = 0; i < tabRegistrationOrder.Count; i++)
        {
            result[i] = tabsByInstance[tabRegistrationOrder[i]];
        }
        orderedTabsCache = result;
        return result;
    }

    public int GetTabIndex(object tab)
    {
        var ordered = GetOrderedTabs();
        for (var i = 0; i < ordered.Length; i++)
        {
            if (ReferenceEquals(ordered[i].Tab, tab))
                return i;
        }
        return -1;
    }

    public TValue? GetFirstEnabledTabValue()
    {
        var firstEnabled = GetFirstEnabledTab();
        return firstEnabled is not null ? firstEnabled.Value : default;
    }
}
