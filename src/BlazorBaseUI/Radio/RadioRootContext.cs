namespace BlazorBaseUI.Radio;

public interface IRadioRootContext
{
    bool Checked { get; }
    bool Disabled { get; }
    bool ReadOnly { get; }
    bool Required { get; }
    RadioRootState State { get; }
}

public sealed record RadioRootContext(
    bool Checked,
    bool Disabled,
    bool ReadOnly,
    bool Required,
    RadioRootState State) : IRadioRootContext
{
    public static RadioRootContext Default { get; } = new(
        Checked: false,
        Disabled: false,
        ReadOnly: false,
        Required: false,
        State: RadioRootState.Default);
}
