namespace BlazorBaseUI.ScrollArea;

internal static class ScrollAreaAttributeHelper
{
    public static void AddRootStateAttributes(Dictionary<string, object> attrs, ScrollAreaRootState state)
    {
        if (state.Scrolling)
        {
            attrs["data-scrolling"] = string.Empty;
        }

        if (state.HasOverflowX)
        {
            attrs["data-has-overflow-x"] = string.Empty;
        }

        if (state.HasOverflowY)
        {
            attrs["data-has-overflow-y"] = string.Empty;
        }

        if (state.OverflowXStart)
        {
            attrs["data-overflow-x-start"] = string.Empty;
        }

        if (state.OverflowXEnd)
        {
            attrs["data-overflow-x-end"] = string.Empty;
        }

        if (state.OverflowYStart)
        {
            attrs["data-overflow-y-start"] = string.Empty;
        }

        if (state.OverflowYEnd)
        {
            attrs["data-overflow-y-end"] = string.Empty;
        }
    }

    public static string CombineClassNames(string requiredClassName, string? userClassName)
    {
        if (string.IsNullOrWhiteSpace(userClassName))
        {
            return requiredClassName;
        }

        return $"{requiredClassName} {userClassName}";
    }
}
