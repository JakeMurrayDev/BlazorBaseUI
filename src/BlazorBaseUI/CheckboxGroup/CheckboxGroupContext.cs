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

public class CheckboxGroupContext : ICheckboxGroupContext
{
    public string[]? Value { get; }
    public string[]? DefaultValue { get; }
    public string[]? AllValues { get; }
    public bool Disabled { get; }
    public CheckboxGroupParent? Parent { get; }
    public FieldValidation? Validation { get; }

    private readonly Func<string[], Task> setValueFunc;
    private readonly Action<CheckboxRoot> registerControlAction;

    public CheckboxGroupContext(
        string[]? value,
        string[]? defaultValue,
        string[]? allValues,
        bool disabled,
        CheckboxGroupParent? parent,
        FieldValidation? validation,
        Func<string[], Task> setValueFunc,
        Action<CheckboxRoot> registerControlAction)
    {
        Value = value;
        DefaultValue = defaultValue;
        AllValues = allValues;
        Disabled = disabled;
        Parent = parent;
        Validation = validation;
        this.setValueFunc = setValueFunc;
        this.registerControlAction = registerControlAction;
    }

    public void SetValue(string[] value) => setValueFunc(value);

    public void RegisterControlRef(CheckboxRoot checkbox) => registerControlAction(checkbox);
}

public class CheckboxGroupParent
{
    private readonly string[] allValues;
    private readonly Func<string[]?> getValue;
    private readonly Func<string[], Task> setValue;
    private readonly Dictionary<string, bool> disabledStates = [];
    private readonly string[] uncontrolledState;

    private ParentCheckboxStatus status = ParentCheckboxStatus.Mixed;

    public string? Id { get; }

    public bool Checked
    {
        get
        {
            var value = getValue() ?? [];
            return value.Length == allValues.Length && allValues.Length > 0;
        }
    }

    public bool Indeterminate
    {
        get
        {
            var value = getValue() ?? [];
            return value.Length > 0 && value.Length < allValues.Length;
        }
    }

    public string? AriaControls =>
        allValues.Length > 0
            ? string.Join(" ", allValues.Select(v => $"{Id}-{v}"))
            : null;

    public CheckboxGroupParent(
        string? id,
        string[] allValues,
        string[]? defaultValue,
        Func<string[]?> getValue,
        Func<string[], Task> setValue)
    {
        Id = id;
        this.allValues = allValues;
        this.getValue = getValue;
        this.setValue = setValue;
        uncontrolledState = defaultValue ?? [];
    }

    public void OnCheckedChange(bool nextChecked)
    {
        var currentValue = getValue() ?? [];

        var none = allValues
            .Where(v => disabledStates.TryGetValue(v, out var disabled) && disabled && currentValue.Contains(v))
            .ToArray();

        var all = allValues
            .Where(v => !disabledStates.TryGetValue(v, out var disabled) || !disabled || currentValue.Contains(v))
            .ToArray();

        var allOnOrOff = currentValue.Length == all.Length || currentValue.Length == 0;

        if (allOnOrOff)
        {
            if (currentValue.Length == all.Length)
            {
                setValue(none);
            }
            else
            {
                setValue(all);
            }
            return;
        }

        if (status == ParentCheckboxStatus.Mixed)
        {
            setValue(all);
            status = ParentCheckboxStatus.On;
        }
        else if (status == ParentCheckboxStatus.On)
        {
            setValue(none);
            status = ParentCheckboxStatus.Off;
        }
        else if (status == ParentCheckboxStatus.Off)
        {
            setValue(uncontrolledState);
            status = ParentCheckboxStatus.Mixed;
        }
    }

    public void OnChildCheckedChange(string childValue, bool nextChecked)
    {
        var currentValue = getValue() ?? [];
        string[] newValue;

        if (nextChecked)
        {
            newValue = [.. currentValue, childValue];
        }
        else
        {
            newValue = currentValue.Where(v => v != childValue).ToArray();
        }

        setValue(newValue);
        status = ParentCheckboxStatus.Mixed;
    }

    public void SetDisabledState(string value, bool disabled)
    {
        disabledStates[value] = disabled;
    }
}