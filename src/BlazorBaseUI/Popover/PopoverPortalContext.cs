namespace BlazorBaseUI.Popover;

internal sealed class PopoverPortalContext
{
    public PopoverPortalContext(bool keepMounted)
    {
        KeepMounted = keepMounted;
    }

    public bool KeepMounted { get; }
}
