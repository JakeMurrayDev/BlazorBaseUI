namespace BlazorBaseUI.Dialog;

public sealed class DialogRootActions
{
    public Action? Unmount { get; internal set; }

    public Action? Close { get; internal set; }

    public Action? Open { get; internal set; }

    public Action<object?>? OpenWithPayload { get; internal set; }
}
