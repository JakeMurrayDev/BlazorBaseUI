namespace BlazorBaseUI.Menu;

/// <summary>
/// Provides extension methods for converting menu enumeration values to their data attribute string representations.
/// </summary>
internal static class Extensions
{
    /// <summary>
    /// Converts an <see cref="MenuInstantType"/> value to its corresponding data attribute string.
    /// </summary>
    /// <param name="instant">The instant type value to convert.</param>
    /// <returns>The data attribute string representation, or <see langword="null"/> if the value is not recognized.</returns>
    public static string? ToDataAttributeString(this MenuInstantType instant) => instant switch
    {
        MenuInstantType.Click => "click",
        MenuInstantType.Dismiss => "dismiss",
        MenuInstantType.Group => "group",
        MenuInstantType.TriggerChange => "trigger-change",
        _ => null
    };

    /// <summary>
    /// Parses a side string from FloatingUI into the corresponding <see cref="Side"/> enum value.
    /// </summary>
    public static Side ParseSide(string value) => value switch
    {
        "top" => Side.Top,
        "right" => Side.Right,
        "bottom" => Side.Bottom,
        "left" => Side.Left,
        _ => Side.Bottom
    };

    /// <summary>
    /// Parses an align string from FloatingUI into the corresponding <see cref="Align"/> enum value.
    /// </summary>
    public static Align ParseAlign(string value) => value switch
    {
        "start" => Align.Start,
        "center" => Align.Center,
        "end" => Align.End,
        _ => Align.Center
    };
}
