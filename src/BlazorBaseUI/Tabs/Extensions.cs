using System.ComponentModel;

namespace BlazorBaseUI.Tabs;

internal static class Extensions
{
    extension(ActivationDirection direction)
    {
        public string ToDataAttributeString() =>
            direction switch
            {
                ActivationDirection.None => "none",
                ActivationDirection.Left => "left",
                ActivationDirection.Right => "right",
                ActivationDirection.Up => "up",
                ActivationDirection.Down => "down",
                _ => throw new InvalidEnumArgumentException(nameof(direction), (int)direction, typeof(ActivationDirection))
            };
    }
}
