using BlazorBaseUI.Field;

namespace BlazorBaseUI.CheckboxGroup;

public record CheckboxGroupState(
    bool Disabled,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    public static CheckboxGroupState Default { get; } = new(
        Disabled: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    public static CheckboxGroupState FromFieldState(
        FieldRootState fieldState,
        bool isDisabled) => new(
            Disabled: isDisabled,
            Valid: fieldState.Valid,
            Touched: fieldState.Touched,
            Dirty: fieldState.Dirty,
            Filled: fieldState.Filled,
            Focused: fieldState.Focused);

    public Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>();

        if (Disabled)
            attributes[CheckboxGroupDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        if (Valid == true)
            attributes[CheckboxGroupDataAttribute.Valid.ToDataAttributeString()] = string.Empty;
        else if (Valid == false)
            attributes[CheckboxGroupDataAttribute.Invalid.ToDataAttributeString()] = string.Empty;

        if (Touched)
            attributes[CheckboxGroupDataAttribute.Touched.ToDataAttributeString()] = string.Empty;

        if (Dirty)
            attributes[CheckboxGroupDataAttribute.Dirty.ToDataAttributeString()] = string.Empty;

        if (Filled)
            attributes[CheckboxGroupDataAttribute.Filled.ToDataAttributeString()] = string.Empty;

        if (Focused)
            attributes[CheckboxGroupDataAttribute.Focused.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}