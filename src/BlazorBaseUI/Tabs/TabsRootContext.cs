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
    void RegisterTabInfo(TValue? value, ElementReference element, string? id, bool disabled);
    void UnregisterTabInfo(TValue? value);
}

public sealed class TabInfo<TValue>(TValue? value, ElementReference element, string? id, bool disabled, int order)
{
    public TValue? Value { get; } = value;
    public ElementReference Element { get; set; } = element;
    public string? Id { get; set; } = id;
    public bool Disabled { get; set; } = disabled;
    public int Order { get; } = order;
}

public sealed class TabsRootContext<TValue> : ITabsRootContext<TValue>
{
    private readonly Dictionary<object, TabInfo<TValue>> tabsByValue = [];
    private readonly Dictionary<object, string> panelIdsByValue = [];
    private int nextOrder;

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

    public Orientation Orientation { get; set; }
    public ActivationDirection ActivationDirection { get; set; }
    public bool HasTabs => tabsByValue.Count > 0;
    public Action? OnTabRegistered { get; set; }
    private Func<TValue?> GetValue { get; }
    private Func<TValue?, ActivationDirection, Task> OnValueChange { get; }

    public TValue? Value => GetValue();

    public async Task OnValueChangeAsync(TValue? value, ActivationDirection direction)
    {
        await OnValueChange(value, direction);
    }

    public void RegisterTabInfo(TValue? value, ElementReference element, string? id, bool disabled)
    {
        if (value is null)
            return;

        if (tabsByValue.TryGetValue(value, out var existing))
        {
            existing.Element = element;
            existing.Id = id;
            existing.Disabled = disabled;
        }
        else
        {
            tabsByValue[value] = new TabInfo<TValue>(value, element, id, disabled, nextOrder++);
        }

        OnTabRegistered?.Invoke();
    }

    public void UnregisterTabInfo(TValue? value)
    {
        if (value is null)
            return;

        tabsByValue.Remove(value!);
    }

    public bool IsTabDisabled(TValue? value)
    {
        if (value is null)
            return false;

        return tabsByValue.TryGetValue(value, out var info) && info.Disabled;
    }

    public TValue? GetFirstEnabledTabValue()
    {
        TabInfo<TValue>? best = null;
        foreach (var info in tabsByValue.Values)
        {
            if (!info.Disabled && (best is null || info.Order < best.Order))
            {
                best = info;
            }
        }
        return best is not null ? best.Value : default;
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
        if (panelValue is not TValue typedValue)
            return null;

        return tabsByValue.TryGetValue(typedValue, out var info) ? info.Id : null;
    }

    public ElementReference? GetTabElementByValue(object? value)
    {
        if (value is not TValue typedValue)
            return null;

        return tabsByValue.TryGetValue(typedValue, out var info) ? info.Element : null;
    }
}

