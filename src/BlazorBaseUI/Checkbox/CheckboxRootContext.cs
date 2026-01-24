namespace BlazorBaseUI.Checkbox;

public sealed record CheckboxRootContext(
    bool Checked,
    bool Disabled,
    bool ReadOnly,
    bool Required,
    bool Indeterminate,
    CheckboxRootState State)
{
    internal static CheckboxRootContext Default { get; } = new(
        Checked: false,
        Disabled: false,
        ReadOnly: false,
        Required: false,
        Indeterminate: false,
        State: CheckboxRootState.Default);
}
