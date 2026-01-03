namespace BlazorBaseUI.Checkbox;

public interface ICheckboxRootContext
{
    bool Checked { get; }
    bool Disabled { get; }
    bool ReadOnly { get; }
    bool Required { get; }
    bool Indeterminate { get; }
    CheckboxRootState State { get; }
}

public sealed record CheckboxRootContext(
    bool Checked,
    bool Disabled,
    bool ReadOnly,
    bool Required,
    bool Indeterminate,
    CheckboxRootState State) : ICheckboxRootContext
{
    public static CheckboxRootContext Default { get; } = new(
        Checked: false,
        Disabled: false,
        ReadOnly: false,
        Required: false,
        Indeterminate: false,
        State: CheckboxRootState.Default);
}