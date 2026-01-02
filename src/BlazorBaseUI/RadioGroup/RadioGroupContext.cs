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

public sealed class RadioRegistration<TValue>
{
    public object Radio { get; }
    public ElementReference Element { get; set; }
    public TValue Value { get; }
    public Func<bool> IsDisabled { get; }
    public Func<ValueTask> Focus { get; }

    public RadioRegistration(object radio, ElementReference element, TValue value, Func<bool> isDisabled, Func<ValueTask> focus)
    {
        Radio = radio;
        Element = element;
        Value = value;
        IsDisabled = isDisabled;
        Focus = focus;
    }
}

public sealed class RadioGroupContext<TValue> : IRadioGroupContext<TValue>
{
    private readonly List<RadioRegistration<TValue>> registeredRadios = [];
    private readonly Func<TValue?> getCheckedValue;
    private readonly Func<TValue, Task> setCheckedValue;

    public bool Disabled { get; private set; }
    public bool ReadOnly { get; private set; }
    public bool Required { get; private set; }
    public string? Name { get; private set; }
    public FieldValidation? Validation { get; private set; }

    public TValue? CheckedValue => getCheckedValue();

    public RadioGroupContext(
        bool disabled,
        bool readOnly,
        bool required,
        string? name,
        FieldValidation? validation,
        Func<TValue?> getCheckedValue,
        Func<TValue, Task> setCheckedValue)
    {
        Disabled = disabled;
        ReadOnly = readOnly;
        Required = required;
        Name = name;
        Validation = validation;
        this.getCheckedValue = getCheckedValue;
        this.setCheckedValue = setCheckedValue;
    }

    /// <summary>
    /// Updates the context properties without losing registered radios.
    /// </summary>
    public void UpdateProperties(
        bool disabled,
        bool readOnly,
        bool required,
        string? name,
        FieldValidation? validation)
    {
        Disabled = disabled;
        ReadOnly = readOnly;
        Required = required;
        Name = name;
        Validation = validation;
    }

    public async Task SetCheckedValueAsync(TValue value)
    {
        await setCheckedValue(value);
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
        await setCheckedValue(registration.Value);
    }
}
