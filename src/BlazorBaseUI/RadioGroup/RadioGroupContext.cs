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

public sealed class RadioRegistration<TValue>(
    object Radio,
    ElementReference Element,
    TValue Value,
    Func<bool> IsDisabled,
    Func<ValueTask> Focus)
{
    public object Radio { get; } = Radio;
    public ElementReference Element { get; set; } = Element;
    public TValue Value { get; } = Value;
    public Func<bool> IsDisabled { get; } = IsDisabled;
    public Func<ValueTask> Focus { get; } = Focus;
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
    private readonly Dictionary<object, RadioRegistration<TValue>> radiosByInstance = [];
    private readonly List<object> registrationOrder = [];
    private RadioRegistration<TValue>[]? orderedRadiosCache;

    public TValue? CheckedValue => GetCheckedValue();

    public async Task SetCheckedValueAsync(TValue value)
    {
        await SetCheckedValue(value);
    }

    public void RegisterRadio(object radio, ElementReference element, TValue value, Func<bool> isDisabled, Func<ValueTask> focus)
    {
        if (radiosByInstance.TryGetValue(radio, out var existing))
        {
            existing.Element = element;
        }
        else
        {
            radiosByInstance[radio] = new RadioRegistration<TValue>(radio, element, value, isDisabled, focus);
            registrationOrder.Add(radio);
        }
        orderedRadiosCache = null;
    }

    public void UnregisterRadio(object radio)
    {
        if (radiosByInstance.Remove(radio))
        {
            registrationOrder.Remove(radio);
            orderedRadiosCache = null;
        }
    }

    public bool IsFirstEnabledRadio(object radio)
    {
        var ordered = GetOrderedRadios();
        for (var i = 0; i < ordered.Length; i++)
        {
            var r = ordered[i];
            if (!r.IsDisabled() && !Disabled)
            {
                return ReferenceEquals(r.Radio, radio);
            }
        }
        return false;
    }

    public RadioRegistration<TValue>? GetFirstEnabledRadio()
    {
        var ordered = GetOrderedRadios();
        for (var i = 0; i < ordered.Length; i++)
        {
            var r = ordered[i];
            if (!r.IsDisabled() && !Disabled)
            {
                return r;
            }
        }
        return null;
    }

    public async Task<bool> NavigateToPreviousAsync(object currentRadio)
    {
        var ordered = GetOrderedRadios();
        var currentIndex = FindRadioIndex(ordered, currentRadio);
        if (currentIndex < 0)
            return false;

        for (var i = currentIndex - 1; i >= 0; i--)
        {
            if (!ordered[i].IsDisabled() && !Disabled)
            {
                await FocusAndSelectRadioAsync(ordered[i]);
                return true;
            }
        }

        for (var i = ordered.Length - 1; i > currentIndex; i--)
        {
            if (!ordered[i].IsDisabled() && !Disabled)
            {
                await FocusAndSelectRadioAsync(ordered[i]);
                return true;
            }
        }

        return false;
    }

    public async Task<bool> NavigateToNextAsync(object currentRadio)
    {
        var ordered = GetOrderedRadios();
        var currentIndex = FindRadioIndex(ordered, currentRadio);
        if (currentIndex < 0)
            return false;

        for (var i = currentIndex + 1; i < ordered.Length; i++)
        {
            if (!ordered[i].IsDisabled() && !Disabled)
            {
                await FocusAndSelectRadioAsync(ordered[i]);
                return true;
            }
        }

        for (var i = 0; i < currentIndex; i++)
        {
            if (!ordered[i].IsDisabled() && !Disabled)
            {
                await FocusAndSelectRadioAsync(ordered[i]);
                return true;
            }
        }

        return false;
    }

    private RadioRegistration<TValue>[] GetOrderedRadios()
    {
        if (orderedRadiosCache is not null)
            return orderedRadiosCache;

        var result = new RadioRegistration<TValue>[registrationOrder.Count];
        for (var i = 0; i < registrationOrder.Count; i++)
        {
            result[i] = radiosByInstance[registrationOrder[i]];
        }
        orderedRadiosCache = result;
        return result;
    }

    private static int FindRadioIndex(RadioRegistration<TValue>[] ordered, object radio)
    {
        for (var i = 0; i < ordered.Length; i++)
        {
            if (ReferenceEquals(ordered[i].Radio, radio))
                return i;
        }
        return -1;
    }

    private async Task FocusAndSelectRadioAsync(RadioRegistration<TValue> registration)
    {
        await registration.Focus();
        await SetCheckedValue(registration.Value);
    }
}
