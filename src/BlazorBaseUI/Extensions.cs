using System.ComponentModel;


namespace BlazorBaseUI;

internal static class Extensions
{
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
}
