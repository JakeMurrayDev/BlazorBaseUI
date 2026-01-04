using BlazorBaseUI.Field;

namespace BlazorBaseUI.RadioGroup;

public sealed record RadioGroupState(
    bool Disabled,
    bool ReadOnly,
    bool Required,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused)
{
    public static RadioGroupState Default { get; } = new(
        Disabled: false,
        ReadOnly: false,
        Required: false,
        Valid: null,
        Touched: false,
        Dirty: false,
        Filled: false,
        Focused: false);

    public static RadioGroupState FromFieldState(
        FieldRootState fieldState,
        bool isDisabled,
        bool isReadOnly,
        bool isRequired) => new(
            Disabled: isDisabled,
            ReadOnly: isReadOnly,
            Required: isRequired,
            Valid: fieldState.Valid,
            Touched: fieldState.Touched,
            Dirty: fieldState.Dirty,
            Filled: fieldState.Filled,
            Focused: fieldState.Focused);

    internal void WriteDataAttributes(Dictionary<string, object> attributes)
    {
        if (Disabled)
            attributes[DataAttributes.Disabled] = string.Empty;

        if (ReadOnly)
            attributes[DataAttributes.ReadOnly] = string.Empty;

        if (Required)
            attributes[DataAttributes.Required] = string.Empty;

        if (Valid == true)
            attributes[DataAttributes.Valid] = string.Empty;
        else if (Valid == false)
            attributes[DataAttributes.Invalid] = string.Empty;

        if (Touched)
            attributes[DataAttributes.Touched] = string.Empty;

        if (Dirty)
            attributes[DataAttributes.Dirty] = string.Empty;

        if (Filled)
            attributes[DataAttributes.Filled] = string.Empty;

        if (Focused)
            attributes[DataAttributes.Focused] = string.Empty;
    }

    private static class DataAttributes
    {
        public const string Disabled = "data-disabled";
        public const string ReadOnly = "data-readonly";
        public const string Required = "data-required";
        public const string Valid = "data-valid";
        public const string Invalid = "data-invalid";
        public const string Touched = "data-touched";
        public const string Dirty = "data-dirty";
        public const string Filled = "data-filled";
        public const string Focused = "data-focused";
    }
}
