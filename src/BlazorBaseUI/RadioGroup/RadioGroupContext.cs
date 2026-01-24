using Microsoft.AspNetCore.Components;
using BlazorBaseUI.Field;

namespace BlazorBaseUI.RadioGroup;

public interface IRadioGroupContext<TValue>
{
    bool Disabled { get; }
    bool ReadOnly { get; }
    bool Required { get; }
    string? Name { get; }
    FieldValidation? Validation { get; }
    ElementReference? GroupElement { get; }
    TValue? CheckedValue { get; }
    Task SetCheckedValueAsync(TValue value);
}

public sealed record RadioGroupContext<TValue> : IRadioGroupContext<TValue>
{
    private readonly Func<TValue?> getCheckedValue;
    private readonly Func<TValue, Task> setCheckedValue;
    private readonly Func<ElementReference?> getGroupElement;

    public RadioGroupContext(
        bool disabled,
        bool readOnly,
        bool required,
        string? name,
        FieldValidation? validation,
        Func<TValue?> getCheckedValue,
        Func<TValue, Task> setCheckedValue,
        Func<ElementReference?> getGroupElement)
    {
        Disabled = disabled;
        ReadOnly = readOnly;
        Required = required;
        Name = name;
        Validation = validation;
        this.getCheckedValue = getCheckedValue;
        this.setCheckedValue = setCheckedValue;
        this.getGroupElement = getGroupElement;
    }

    public bool Disabled { get; set; }
    public bool ReadOnly { get; set; }
    public bool Required { get; set; }
    public string? Name { get; set; }
    public FieldValidation? Validation { get; set; }

    public TValue? CheckedValue => getCheckedValue();
    public ElementReference? GroupElement => getGroupElement();

    public async Task SetCheckedValueAsync(TValue value)
    {
        await setCheckedValue(value);
    }
}
