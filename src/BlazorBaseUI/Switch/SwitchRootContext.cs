namespace BlazorBaseUI.Switch;

public interface ISwitchRootContext
{
    bool Checked { get; }
    bool Disabled { get; }
    bool ReadOnly { get; }
    bool Required { get; }
    SwitchRootState State { get; }
}

public record SwitchRootContext(
    bool Checked,
    bool Disabled,
    bool ReadOnly,
    bool Required,
    SwitchRootState State) : ISwitchRootContext
{
    public static SwitchRootContext Default { get; } = new(
        Checked: false,
        Disabled: false,
        ReadOnly: false,
        Required: false,
        State: SwitchRootState.Default);
}