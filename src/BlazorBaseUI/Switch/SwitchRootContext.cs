namespace BlazorBaseUI.Switch;

public sealed record SwitchRootContext(
    bool Checked,
    bool Disabled,
    bool ReadOnly,
    bool Required,
    SwitchRootState State)
{
    internal static SwitchRootContext Default { get; } = new(
        Checked: false,
        Disabled: false,
        ReadOnly: false,
        Required: false,
        State: SwitchRootState.Default);
}
