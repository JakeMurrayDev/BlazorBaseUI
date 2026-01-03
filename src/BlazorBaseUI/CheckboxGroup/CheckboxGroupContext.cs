using BlazorBaseUI.Field;
using BlazorBaseUI.Checkbox;

namespace BlazorBaseUI.CheckboxGroup;

public interface ICheckboxGroupContext
{
    string[]? Value { get; }
    string[]? DefaultValue { get; }
    string[]? AllValues { get; }
    bool Disabled { get; }
    CheckboxGroupParent? Parent { get; }
    FieldValidation? Validation { get; }
    void SetValue(string[] value);
    void RegisterControlRef(CheckboxRoot checkbox);
}

public sealed record CheckboxGroupContext(
    string[]? Value,
    string[]? DefaultValue,
    string[]? AllValues,
    bool Disabled,
    CheckboxGroupParent? Parent,
    FieldValidation? Validation,
    Func<string[], Task> SetValueFunc,
    Action<CheckboxRoot> RegisterControlAction) : ICheckboxGroupContext
{
    public void SetValue(string[] value) => SetValueFunc(value);
    public void RegisterControlRef(CheckboxRoot checkbox) => RegisterControlAction(checkbox);
}

public sealed record CheckboxGroupParent(
    string? Id,
    string[] AllValues,
    string[]? DefaultValue,
    Func<string[]?> GetValue,
    Func<string[], Task> SetValue)
{
    private readonly Dictionary<string, bool> disabledStates = [];
    private readonly string[] uncontrolledState = DefaultValue ?? [];

    private ParentCheckboxStatus status = ParentCheckboxStatus.Mixed;

    public bool Checked
    {
        get
        {
            var value = GetValue() ?? [];
            return value.Length == AllValues.Length && AllValues.Length > 0;
        }
    }

    public bool Indeterminate
    {
        get
        {
            var value = GetValue() ?? [];
            return value.Length > 0 && value.Length < AllValues.Length;
        }
    }

    public string? AriaControls =>
        AllValues.Length > 0
            ? string.Join(" ", AllValues.Select(v => $"{Id}-{v}"))
            : null;

    public void OnCheckedChange(bool nextChecked)
    {
        var currentValue = GetValue() ?? [];

        var none = AllValues
            .Where(v => disabledStates.TryGetValue(v, out var disabled) && disabled && currentValue.Contains(v))
            .ToArray();

        var all = AllValues
            .Where(v => !disabledStates.TryGetValue(v, out var disabled) || !disabled || currentValue.Contains(v))
            .ToArray();

        var allOnOrOff = currentValue.Length == all.Length || currentValue.Length == 0;

        if (allOnOrOff)
        {
            if (currentValue.Length == all.Length)
            {
                SetValue(none);
            }
            else
            {
                SetValue(all);
            }
            return;
        }

        if (status == ParentCheckboxStatus.Mixed)
        {
            SetValue(all);
            status = ParentCheckboxStatus.On;
        }
        else if (status == ParentCheckboxStatus.On)
        {
            SetValue(none);
            status = ParentCheckboxStatus.Off;
        }
        else if (status == ParentCheckboxStatus.Off)
        {
            SetValue(uncontrolledState);
            status = ParentCheckboxStatus.Mixed;
        }
    }

    public void OnChildCheckedChange(string childValue, bool nextChecked)
    {
        var currentValue = GetValue() ?? [];
        string[] newValue;

        if (nextChecked)
        {
            newValue = [.. currentValue, childValue];
        }
        else
        {
            newValue = currentValue.Where(v => v != childValue).ToArray();
        }

        SetValue(newValue);
        status = ParentCheckboxStatus.Mixed;
    }

    public void SetDisabledState(string value, bool disabled)
    {
        disabledStates[value] = disabled;
    }
}