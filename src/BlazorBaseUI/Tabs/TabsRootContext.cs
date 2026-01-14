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
    void RegisterTabInfo(TValue? value, ElementReference element, string? id);
    void UnregisterTabInfo(TValue? value);
}

public sealed class TabInfo<TValue>(TValue? value, ElementReference element, string? id)
{
    public TValue? Value { get; } = value;
    public ElementReference Element { get; set; } = element;
    public string? Id { get; set; } = id;
}

public sealed class TabsRootContext<TValue> : ITabsRootContext<TValue>
{
    private readonly Dictionary<object, TabInfo<TValue>> tabsByValue = [];
    private readonly Dictionary<object, string> panelIdsByValue = [];

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
    private Func<TValue?> GetValue { get; }
    private Func<TValue?, ActivationDirection, Task> OnValueChange { get; }

    public TValue? Value => GetValue();

    public async Task OnValueChangeAsync(TValue? value, ActivationDirection direction)
    {
        await OnValueChange(value, direction);
    }

    public void RegisterTabInfo(TValue? value, ElementReference element, string? id)
    {
        if (value is null)
            return;

        if (tabsByValue.TryGetValue(value, out var existing))
        {
            existing.Element = element;
            existing.Id = id;
        }
        else
        {
            tabsByValue[value] = new TabInfo<TValue>(value, element, id);
        }
    }

    public void UnregisterTabInfo(TValue? value)
    {
        if (value is null)
            return;

        tabsByValue.Remove(value!);
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

