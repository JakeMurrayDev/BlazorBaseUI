using System.Text.RegularExpressions;

namespace BlazorBaseUI.NumberField;

/// <summary>
/// The native input control in the number field.
/// Renders an <c>&lt;input&gt;</c> element with <c>type="text"</c>.
/// </summary>
public sealed partial class NumberFieldInput
{
    [GeneratedRegex(@"[\u0660-\u0669]")]
    private static partial Regex ArabicIndicRegex();

    [GeneratedRegex(@"[\u06F0-\u06F9]")]
    private static partial Regex ExtendedArabicIndicRegex();

    [GeneratedRegex(@"[\uFF10-\uFF19]")]
    private static partial Regex FullwidthRegex();

    [GeneratedRegex(@"[\u3007\u4E00\u4E8C\u4E09\u56DB\u4E94\u516D\u4E03\u516B\u4E5D]")]
    private static partial Regex HanRegex();
}
