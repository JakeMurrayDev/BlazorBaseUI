namespace BlazorBaseUI.Field;

public readonly record struct FieldRootState(
    bool Disabled,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    public static FieldRootState Default { get; } = new(
        Disabled: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    private static readonly Dictionary<string, object> EmptyAttributes = new(0);
    private static readonly object EmptyValue = string.Empty;

    internal Dictionary<string, object> GetDataAttributes()
    {
        var hasDisabled = Disabled;
        var hasValid = Valid == true;
        var hasInvalid = Valid == false;
        var hasTouched = Touched;
        var hasDirty = Dirty;
        var hasFilled = Filled;
        var hasFocused = Focused;

        var count = (hasDisabled ? 1 : 0) +
                    (hasValid ? 1 : 0) +
                    (hasInvalid ? 1 : 0) +
                    (hasTouched ? 1 : 0) +
                    (hasDirty ? 1 : 0) +
                    (hasFilled ? 1 : 0) +
                    (hasFocused ? 1 : 0);

        if (count == 0)
            return EmptyAttributes;

        var attributes = new Dictionary<string, object>(count);

        if (hasDisabled)
            attributes["data-disabled"] = EmptyValue;

        if (hasValid)
            attributes["data-valid"] = EmptyValue;
        else if (hasInvalid)
            attributes["data-invalid"] = EmptyValue;

        if (hasTouched)
            attributes["data-touched"] = EmptyValue;

        if (hasDirty)
            attributes["data-dirty"] = EmptyValue;

        if (hasFilled)
            attributes["data-filled"] = EmptyValue;

        if (hasFocused)
            attributes["data-focused"] = EmptyValue;

        return attributes;
    }
}
