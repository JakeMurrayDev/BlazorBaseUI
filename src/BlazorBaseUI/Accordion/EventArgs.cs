namespace BlazorBaseUI.Accordion;

/// <summary>
/// Provides data for the accordion value change event.
/// </summary>
/// <typeparam name="TValue">The type of the value used to identify accordion items.</typeparam>
public sealed class AccordionValueChangeEventArgs<TValue> : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccordionValueChangeEventArgs{TValue}"/> class.
    /// </summary>
    /// <param name="value">The new value of the expanded item(s).</param>
    public AccordionValueChangeEventArgs(TValue[] value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the new value of the expanded item(s).
    /// </summary>
    public TValue[] Value { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the value change should be canceled.
    /// </summary>
    public bool Canceled { get; set; }
}
