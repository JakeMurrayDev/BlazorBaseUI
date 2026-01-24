namespace BlazorBaseUI.Slider;

public enum ThumbCollisionBehavior
{
    Push,
    Swap,
    None
}

public enum ThumbAlignment
{
    Center,
    Edge
}

public enum SliderChangeReason
{
    None,
    InputChange,
    TrackPress,
    Drag,
    Keyboard
}
