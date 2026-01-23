namespace BlazorBaseUI.Radio;

public sealed record RadioRootContext(
    bool Checked,
    bool Disabled,
    bool ReadOnly,
    bool Required,
    RadioRootState State);
