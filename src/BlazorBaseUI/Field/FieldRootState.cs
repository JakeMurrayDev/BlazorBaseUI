namespace BlazorBaseUI.Field;

public record FieldRootState(
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

    internal Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>();

        if (Disabled)
            attributes[FieldDataAttribute.Disabled.ToDataAttributeName()] = string.Empty;

        if (Valid == true)
            attributes[FieldDataAttribute.Valid.ToDataAttributeName()] = string.Empty;
        else if (Valid == false)
            attributes[FieldDataAttribute.Invalid.ToDataAttributeName()] = string.Empty;

        if (Touched)
            attributes[FieldDataAttribute.Touched.ToDataAttributeName()] = string.Empty;

        if (Dirty)
            attributes[FieldDataAttribute.Dirty.ToDataAttributeName()] = string.Empty;

        if (Filled)
            attributes[FieldDataAttribute.Filled.ToDataAttributeName()] = string.Empty;

        if (Focused)
            attributes[FieldDataAttribute.Focused.ToDataAttributeName()] = string.Empty;

        return attributes;
    }
}