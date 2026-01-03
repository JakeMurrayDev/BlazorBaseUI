using Microsoft.AspNetCore.Components;
using BlazorBaseUI.Field;

namespace BlazorBaseUI.RadioGroup;

public interface IRadioGroupContext
{
    bool Disabled { get; }
    bool ReadOnly { get; }
    bool Required { get; }
    string? Name { get; }
    FieldValidation? Validation { get; }
}

public interface IRadioGroupContext<TValue> : IRadioGroupContext
{
    TValue? CheckedValue { get; }
    Task SetCheckedValueAsync(TValue value);
    void RegisterRadio(object radio, ElementReference element, TValue value, Func<bool> isDisabled, Func<ValueTask> focus);
    void UnregisterRadio(object radio);
    bool IsFirstEnabledRadio(object radio);
    Task<bool> NavigateToPreviousAsync(object currentRadio);
    Task<bool> NavigateToNextAsync(object currentRadio);
    RadioRegistration<TValue>? GetFirstEnabledRadio();
}

public sealed record RadioRegistration<TValue>(
    object Radio,
    ElementReference Element,
    TValue Value,
    Func<bool> IsDisabled,
    Func<ValueTask> Focus)
{
    public ElementReference Element { get; set; } = Element;
}

public sealed record RadioGroupContext<TValue>(
    bool Disabled,
    bool ReadOnly,
    bool Required,
    string? Name,
    FieldValidation? Validation,
    Func<TValue?> GetCheckedValue,
    Func<TValue, Task> SetCheckedValue) : IRadioGroupContext<TValue>
{
    private readonly List<RadioRegistration<TValue>> registeredRadios = [];

    public TValue? CheckedValue => GetCheckedValue();

    public async Task SetCheckedValueAsync(TValue value)
    {
        await SetCheckedValue(value);
    }

    public void RegisterRadio(object radio, ElementReference element, TValue value, Func<bool> isDisabled, Func<ValueTask> focus)
    {
        var existing = registeredRadios.FindIndex(r => ReferenceEquals(r.Radio, radio));
        if (existing >= 0)
        {
            registeredRadios[existing].Element = element;
        }
        else
        {
            registeredRadios.Add(new RadioRegistration<TValue>(radio, element, value, isDisabled, focus));
        }
    }

    public void UnregisterRadio(object radio)
    {
        registeredRadios.RemoveAll(r => ReferenceEquals(r.Radio, radio));
    }

    public bool IsFirstEnabledRadio(object radio)
    {
        var firstEnabled = registeredRadios.FirstOrDefault(r => !r.IsDisabled() && !Disabled);
        return firstEnabled is not null && ReferenceEquals(firstEnabled.Radio, radio);
    }

    public RadioRegistration<TValue>? GetFirstEnabledRadio()
    {
        return registeredRadios.FirstOrDefault(r => !r.IsDisabled() && !Disabled);
    }

    public async Task<bool> NavigateToPreviousAsync(object currentRadio)
    {
        var currentIndex = registeredRadios.FindIndex(r => ReferenceEquals(r.Radio, currentRadio));
        if (currentIndex < 0)
            return false;

        for (var i = currentIndex - 1; i >= 0; i--)
        {
            if (!registeredRadios[i].IsDisabled() && !Disabled)
            {
                await FocusAndSelectRadioAsync(i);
                return true;
            }
        }

        for (var i = registeredRadios.Count - 1; i > currentIndex; i--)
        {
            if (!registeredRadios[i].IsDisabled() && !Disabled)
            {
                await FocusAndSelectRadioAsync(i);
                return true;
            }
        }

        return false;
    }

    public async Task<bool> NavigateToNextAsync(object currentRadio)
    {
        var currentIndex = registeredRadios.FindIndex(r => ReferenceEquals(r.Radio, currentRadio));
        if (currentIndex < 0)
            return false;

        for (var i = currentIndex + 1; i < registeredRadios.Count; i++)
        {
            if (!registeredRadios[i].IsDisabled() && !Disabled)
            {
                await FocusAndSelectRadioAsync(i);
                return true;
            }
        }

        for (var i = 0; i < currentIndex; i++)
        {
            if (!registeredRadios[i].IsDisabled() && !Disabled)
            {
                await FocusAndSelectRadioAsync(i);
                return true;
            }
        }

        return false;
    }

    private async Task FocusAndSelectRadioAsync(int index)
    {
        var registration = registeredRadios[index];
        await registration.Focus();
        await SetCheckedValue(registration.Value);
    }
}
