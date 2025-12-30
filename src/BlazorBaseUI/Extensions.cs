using System.ComponentModel;
using System.Globalization;


namespace BlazorBaseUI;

internal static class Extensions
{
    extension (Guid value)
    {
        public string ToIdString() =>
            value.ToString("N", CultureInfo.InvariantCulture)[..8];
    }

    extension(Orientation orientation)
    {
        public string ToDataAttributeString() =>
            orientation switch
            {
                Orientation.Horizontal => "horizontal",
                Orientation.Vertical => "vertical",
                _ => throw new InvalidEnumArgumentException(nameof(orientation), (int)orientation, typeof(Orientation))
            };
    }

    extension(TransitionStatus status)
    {
        public string? ToDataAttributeString() =>
            status switch
            {
                TransitionStatus.Undefined => null,
                TransitionStatus.Starting => "starting",
                TransitionStatus.Ending => "ending",
                TransitionStatus.Idle => "idle",
                _ => throw new InvalidEnumArgumentException(nameof(status), (int)status, typeof(TransitionStatus))
            };
    }
}
