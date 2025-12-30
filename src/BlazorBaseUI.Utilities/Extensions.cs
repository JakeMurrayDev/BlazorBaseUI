using System.Globalization;

namespace BlazorBaseUI.Utilities;

internal static class Extensions
{
    extension(Guid value)
    {
        public string ToIdString() =>
            value.ToString("N", CultureInfo.InvariantCulture)[..8];
    }
}
