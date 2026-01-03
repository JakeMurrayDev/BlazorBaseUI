namespace BlazorBaseUI.Toggle;

public class TogglePressedChangeEventArgs : EventArgs
{
    public bool Pressed { get; }
    public bool IsCanceled { get; private set; }

    public TogglePressedChangeEventArgs(bool pressed)
    {
        Pressed = pressed;
    }

    public void Cancel() => IsCanceled = true;
}
