namespace BlazorBaseUI.Menu;

/// <summary>
/// Provides extension methods for converting menu enumeration values to their data attribute string representations.
/// </summary>
internal static class Extensions
{
    /// <summary>
    /// Converts an <see cref="InstantType"/> value to its corresponding data attribute string.
    /// </summary>
    /// <param name="instant">The instant type value to convert.</param>
    /// <returns>The data attribute string representation, or <see langword="null"/> if the value is not recognized.</returns>
    public static string? ToDataAttributeString(this InstantType instant) => instant switch
    {
        InstantType.Click => "click",
        InstantType.Dismiss => "dismiss",
        InstantType.Group => "group",
        _ => null
    };
}
