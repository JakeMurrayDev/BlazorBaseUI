namespace BlazorBaseUI.Select;

/// <summary>
/// Provides data for the <see cref="SelectRoot{TValue}.OnOpenChange"/> event.
/// </summary>
public sealed class SelectOpenChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectOpenChangeEventArgs"/> class.
    /// </summary>
    public SelectOpenChangeEventArgs(bool open, SelectOpenChangeReason reason)
    {
        Open = open;
        Reason = reason;
    }

    /// <summary>
    /// Gets whether the select is being opened or closed.
    /// </summary>
    public bool Open { get; }

    /// <summary>
    /// Gets the reason the select's open state changed.
    /// </summary>
    public SelectOpenChangeReason Reason { get; }

    /// <summary>
    /// Gets whether the open change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels the open state change, preventing the select from opening or closing.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}

/// <summary>
/// Provides data for the <see cref="SelectRoot{TValue}.OnValueChange"/> event.
/// </summary>
/// <typeparam name="TValue">The type of value used by the select.</typeparam>
public sealed class SelectValueChangeEventArgs<TValue> : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectValueChangeEventArgs{TValue}"/> class.
    /// </summary>
    public SelectValueChangeEventArgs(TValue? value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the newly selected value.
    /// </summary>
    public TValue? Value { get; }

    /// <summary>
    /// Gets whether the value change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels the value change, preventing the selected value from updating.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}
