namespace BlazorBaseUI.Tabs;

/// <summary>
/// Provides data for the <see cref="TabsRoot{TValue}.OnValueChange"/> event,
/// including the ability to cancel the tab change.
/// </summary>
/// <typeparam name="TValue">The type of value used to identify tabs.</typeparam>
public sealed class TabsValueChangeEventArgs<TValue>(TValue? value, ActivationDirection activationDirection)
{
    /// <summary>
    /// Gets the new tab value being activated.
    /// </summary>
    public TValue? Value { get; } = value;

    /// <summary>
    /// Gets the direction of the activation relative to the previously active tab.
    /// </summary>
    public ActivationDirection ActivationDirection { get; } = activationDirection;

    /// <summary>
    /// Gets a value indicating whether the tab change has been canceled.
    /// </summary>
    public bool IsCanceled { get; private set; }

    /// <summary>
    /// Cancels the tab change, preventing the active tab from updating.
    /// </summary>
    public void Cancel() => IsCanceled = true;
}
