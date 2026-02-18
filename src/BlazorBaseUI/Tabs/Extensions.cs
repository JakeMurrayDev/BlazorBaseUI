using System.ComponentModel;

namespace BlazorBaseUI.Tabs;

/// <summary>
/// Provides extension methods for Tabs-related types.
/// </summary>
internal static class Extensions
{
    extension(ActivationDirection direction)
    {
        /// <summary>
        /// Converts the <see cref="ActivationDirection"/> to its lowercase string representation
        /// for use in HTML data attributes.
        /// </summary>
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
