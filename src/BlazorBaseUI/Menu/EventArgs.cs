namespace BlazorBaseUI.Menu;

/// <summary>
/// Provides data for the <see cref="MenuRoot.OnOpenChange"/> event.
/// </summary>
public sealed class MenuOpenChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MenuOpenChangeEventArgs"/> class.
    /// </summary>
    public MenuOpenChangeEventArgs(bool open, OpenChangeReason reason, object? payload = null)
    {
        Open = open;
        Reason = reason;
        Payload = payload;
    }

    /// <summary>
    /// Gets whether the menu is being opened or closed.
    /// </summary>
    public bool Open { get; }

    /// <summary>
    /// Gets the reason the menu's open state changed.
    /// </summary>
    public OpenChangeReason Reason { get; }

    /// <summary>
    /// Gets the optional payload associated with the state change.
    /// </summary>
    public object? Payload { get; }

    /// <summary>
    /// Gets whether the open change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels the open state change, preventing the menu from opening or closing.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}

/// <summary>
/// Provides data for the <see cref="MenuRadioGroup.OnValueChange"/> event.
/// </summary>
public sealed class MenuRadioGroupChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MenuRadioGroupChangeEventArgs"/> class.
    /// </summary>
    public MenuRadioGroupChangeEventArgs(object? value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the newly selected value.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets whether the value change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels the value change, preventing the selection from updating.
    /// </summary>
    public void Cancel()
    {
        IsCanceled = true;
    }
}

/// <summary>
/// Provides data for the <see cref="MenuCheckboxItem.OnCheckedChange"/> event.
/// </summary>
public sealed class MenuCheckboxItemChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MenuCheckboxItemChangeEventArgs"/> class.
    /// </summary>
    public MenuCheckboxItemChangeEventArgs(bool @checked)
    {
        Checked = @checked;
    }

    /// <summary>
    /// Gets the new checked state.
    /// </summary>
    public bool Checked { get; }

    /// <summary>
    /// Gets whether the checked state change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels the checked state change, preventing the checkbox from toggling.
    /// </summary>
    public void Cancel()
    {
        IsCanceled = true;
    }
}
