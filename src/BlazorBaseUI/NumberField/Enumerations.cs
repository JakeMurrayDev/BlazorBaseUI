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

internal static class NumberFieldExtensions
{
    extension(ScrubDirection direction)
    {
        public string ToDataAttributeString() =>
            direction switch
            {
                ScrubDirection.Horizontal => "horizontal",
                ScrubDirection.Vertical => "vertical",
                _ => "horizontal"
            };
    }

    extension(NumberFieldChangeReason reason)
    {
        public string ToReasonString() =>
            reason switch
            {
                NumberFieldChangeReason.None => "none",
                NumberFieldChangeReason.InputChange => "input-change",
                NumberFieldChangeReason.InputClear => "input-clear",
                NumberFieldChangeReason.InputBlur => "input-blur",
                NumberFieldChangeReason.InputPaste => "input-paste",
                NumberFieldChangeReason.Keyboard => "keyboard",
                NumberFieldChangeReason.IncrementPress => "increment-press",
                NumberFieldChangeReason.DecrementPress => "decrement-press",
                NumberFieldChangeReason.Wheel => "wheel",
                NumberFieldChangeReason.Scrub => "scrub",
                _ => "none"
            };
    }
}
