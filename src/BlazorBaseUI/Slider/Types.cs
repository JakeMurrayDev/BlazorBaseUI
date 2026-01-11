namespace BlazorBaseUI.Slider;

internal sealed record ThumbRect(double Left, double Right, double Top, double Bottom, double Width, double Height, double MidX, double MidY);

internal sealed record StartDragResult(int ThumbIndex, double[] Values);

internal sealed record SliderDragConfig(double Min, double Max, double Step, int MinStepsBetweenValues, string Orientation, string Direction, string CollisionBehavior, string ThumbAlignment, double[] Values, bool Disabled, bool ReadOnly, double InsetOffset, bool NotifyOnMove);
