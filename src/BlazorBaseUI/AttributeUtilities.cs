using System.Globalization;

namespace BlazorBaseUI;

/// <summary>
/// Provides helper methods for reading, combining, and resolving HTML attributes
/// from <see cref="IReadOnlyDictionary{TKey, TValue}"/> parameter collections.
/// </summary>
internal static class AttributeUtilities
{
    public static T? GetAttributeValue<T>(
        IReadOnlyDictionary<string, object>? attributes,
        string attribute
    )
    {
        if (
            attributes is null
            || !attributes.TryGetValue(attribute, out var value)
        )
        {
            return default;
        }
        
        switch (value)
        {
            case null:
                return default;
            case T typedValue:
                return typedValue;
            default:
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    throw new InvalidCastException(
                        $"Cannot convert attribute '{attribute}' of type "
                        + $"{value.GetType().Name} to {typeof(T).Name}"
                    );
                }
        }
    }

    public static string? GetAttributeStringValue(
        IReadOnlyDictionary<string, object>? attributes,
        string attribute
    )
    {
        if (
            attributes is null
            || !attributes.TryGetValue(attribute, out var value)
        )
        {
            return default;
        }

        var attributeValue = Convert.ToString(
            value,
            CultureInfo.InvariantCulture
        );

        return attributeValue;
    }

    public static bool HasAttribute(
        IReadOnlyDictionary<string, object>? attributes, 
        string attribute
    )
    {
        return attributes is not null && attributes.TryGetValue(attribute, out var _);
    }

    public static string? CombineClassNames(
        IReadOnlyDictionary<string, object>? attributes,
        string? classNames
    )
    {
        if (
            attributes is null
            || !attributes.TryGetValue("class", out var classValue)
        )
        {
            return classNames;
        }

        var classAttributeValue = Convert.ToString(
            classValue,
            CultureInfo.InvariantCulture
        );

        if (string.IsNullOrEmpty(classAttributeValue))
            return classNames;

        if (string.IsNullOrEmpty(classNames))
            return classAttributeValue;

        return $"{classAttributeValue} {classNames}";
    }

    public static string? CombineStyles(
        IReadOnlyDictionary<string, object>? attributes,
        string? styles
    )
    {
        if (
            attributes is null
            || !attributes.TryGetValue("style", out var styleValue)
        )
        {
            return styles;
        }

        var styleAttributeValue = Convert.ToString(
            styleValue,
            CultureInfo.InvariantCulture
        );

        if (string.IsNullOrEmpty(styleAttributeValue))
            return styles;

        if (string.IsNullOrEmpty(styles))
            return styleAttributeValue;

        var separator =
            styleAttributeValue.TrimEnd().EndsWith(';') ? " " : "; ";

        return $"{styleAttributeValue}{separator}{styles}";
    }

    public static string GetIdOrDefault(
        IReadOnlyDictionary<string, object>? attributes,
        Func<string> defaultId
    )
    {
        if (
            attributes is null
            || !attributes.TryGetValue("id", out var idValue)
        )
        {
            return defaultId();
        }

        var idAttributeValue = Convert.ToString(
            idValue,
            CultureInfo.InvariantCulture
        );

        return string.IsNullOrEmpty(idAttributeValue)
            ? defaultId()
            : idAttributeValue;
    }
}                                     