namespace BlazorBaseUI.Tabs;

public sealed class TabsValueChangeEventArgs<TValue>(TValue? value, ActivationDirection activationDirection)
{
    public TValue? Value { get; } = value;

    public ActivationDirection ActivationDirection { get; } = activationDirection;

    public bool IsCanceled { get; private set; }

    public void Cancel() => IsCanceled = true;
}
