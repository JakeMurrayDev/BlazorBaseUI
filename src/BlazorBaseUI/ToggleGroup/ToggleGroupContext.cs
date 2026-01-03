using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.ToggleGroup;

public interface IToggleGroupContext
{
    IReadOnlyList<string> Value { get; }
    bool Disabled { get; }
    Orientation Orientation { get; }
    bool LoopFocus { get; }
    Task SetGroupValueAsync(string toggleValue, bool nextPressed);
    void RegisterToggle(object toggle, ElementReference element, string value, Func<bool> isDisabled, Func<ValueTask> focus);
    void UnregisterToggle(object toggle);
    bool IsFirstEnabledToggle(object toggle);
    Task<bool> NavigateToPreviousAsync(object currentToggle);
    Task<bool> NavigateToNextAsync(object currentToggle);
    Task NavigateToFirstAsync();
    Task NavigateToLastAsync();
}

public sealed class ToggleRegistration
{
    public object Toggle { get; }
    public ElementReference Element { get; set; }
    public string Value { get; }
    public Func<bool> IsDisabled { get; }
    public Func<ValueTask> Focus { get; }

    public ToggleRegistration(object toggle, ElementReference element, string value, Func<bool> isDisabled, Func<ValueTask> focus)
    {
        Toggle = toggle;
        Element = element;
        Value = value;
        IsDisabled = isDisabled;
        Focus = focus;
    }
}

public sealed class ToggleGroupContext : IToggleGroupContext
{
    private readonly List<ToggleRegistration> registeredToggles = [];
    private readonly Func<IReadOnlyList<string>> getValue;
    private readonly Func<string, bool, Task> setGroupValue;

    public bool Disabled { get; private set; }
    public Orientation Orientation { get; private set; }
    public bool LoopFocus { get; private set; }

    public IReadOnlyList<string> Value => getValue();

    public ToggleGroupContext(
        bool disabled,
        Orientation orientation,
        bool loopFocus,
        Func<IReadOnlyList<string>> getValue,
        Func<string, bool, Task> setGroupValue)
    {
        Disabled = disabled;
        Orientation = orientation;
        LoopFocus = loopFocus;
        this.getValue = getValue;
        this.setGroupValue = setGroupValue;
    }

    public void UpdateProperties(bool disabled, Orientation orientation, bool loopFocus)
    {
        Disabled = disabled;
        Orientation = orientation;
        LoopFocus = loopFocus;
    }

    public async Task SetGroupValueAsync(string toggleValue, bool nextPressed)
    {
        await setGroupValue(toggleValue, nextPressed);
    }

    public void RegisterToggle(object toggle, ElementReference element, string value, Func<bool> isDisabled, Func<ValueTask> focus)
    {
        var existing = registeredToggles.FindIndex(r => ReferenceEquals(r.Toggle, toggle));
        if (existing >= 0)
        {
            registeredToggles[existing].Element = element;
        }
        else
        {
            registeredToggles.Add(new ToggleRegistration(toggle, element, value, isDisabled, focus));
        }
    }

    public void UnregisterToggle(object toggle)
    {
        registeredToggles.RemoveAll(r => ReferenceEquals(r.Toggle, toggle));
    }

    public bool IsFirstEnabledToggle(object toggle)
    {
        var firstEnabled = registeredToggles.FirstOrDefault(r => !r.IsDisabled() && !Disabled);
        return firstEnabled is not null && ReferenceEquals(firstEnabled.Toggle, toggle);
    }

    public async Task<bool> NavigateToPreviousAsync(object currentToggle)
    {
        var currentIndex = registeredToggles.FindIndex(r => ReferenceEquals(r.Toggle, currentToggle));
        if (currentIndex < 0)
            return false;

        for (var i = currentIndex - 1; i >= 0; i--)
        {
            if (!registeredToggles[i].IsDisabled() && !Disabled)
            {
                await registeredToggles[i].Focus();
                return true;
            }
        }

        if (LoopFocus)
        {
            for (var i = registeredToggles.Count - 1; i > currentIndex; i--)
            {
                if (!registeredToggles[i].IsDisabled() && !Disabled)
                {
                    await registeredToggles[i].Focus();
                    return true;
                }
            }
        }

        return false;
    }

    public async Task<bool> NavigateToNextAsync(object currentToggle)
    {
        var currentIndex = registeredToggles.FindIndex(r => ReferenceEquals(r.Toggle, currentToggle));
        if (currentIndex < 0)
            return false;

        for (var i = currentIndex + 1; i < registeredToggles.Count; i++)
        {
            if (!registeredToggles[i].IsDisabled() && !Disabled)
            {
                await registeredToggles[i].Focus();
                return true;
            }
        }

        if (LoopFocus)
        {
            for (var i = 0; i < currentIndex; i++)
            {
                if (!registeredToggles[i].IsDisabled() && !Disabled)
                {
                    await registeredToggles[i].Focus();
                    return true;
                }
            }
        }

        return false;
    }

    public async Task NavigateToFirstAsync()
    {
        for (var i = 0; i < registeredToggles.Count; i++)
        {
            if (!registeredToggles[i].IsDisabled() && !Disabled)
            {
                await registeredToggles[i].Focus();
                return;
            }
        }
    }

    public async Task NavigateToLastAsync()
    {
        for (var i = registeredToggles.Count - 1; i >= 0; i--)
        {
            if (!registeredToggles[i].IsDisabled() && !Disabled)
            {
                await registeredToggles[i].Focus();
                return;
            }
        }
    }
}
