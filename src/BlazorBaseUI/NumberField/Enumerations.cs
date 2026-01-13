namespace BlazorBaseUI.NumberField;

public enum ScrubDirection
{
    Horizontal,
    Vertical
}

public enum NumberFieldChangeReason
{
    None,
    InputChange,
    InputClear,
    InputBlur,
    InputPaste,
    Keyboard,
    IncrementPress,
    DecrementPress,
    Wheel,
    Scrub
}