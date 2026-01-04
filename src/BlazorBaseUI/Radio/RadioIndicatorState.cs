namespace BlazorBaseUI.Radio;

public sealed record RadioIndicatorState(
    bool Checked,
    bool Disabled,
    bool ReadOnly,
    bool Required,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused,
    TransitionStatus TransitionStatus)
{
    internal void WriteDataAttributes(Dictionary<string, object> attributes)
    {
        if (Checked)
            attributes[DataAttributes.Checked] = string.Empty;
        else
            attributes[DataAttributes.Unchecked] = string.Empty;

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

        if (TransitionStatus == TransitionStatus.Starting)
            attributes[DataAttributes.StartingStyle] = string.Empty;
        else if (TransitionStatus == TransitionStatus.Ending)
            attributes[DataAttributes.EndingStyle] = string.Empty;
    }

    private static class DataAttributes
    {
        public const string Checked = "data-checked";
        public const string Unchecked = "data-unchecked";
        public const string Disabled = "data-disabled";
        public const string ReadOnly = "data-readonly";
        public const string Required = "data-required";
        public const string Valid = "data-valid";
        public const string Invalid = "data-invalid";
        public const string Touched = "data-touched";
        public const string Dirty = "data-dirty";
        public const string Filled = "data-filled";
        public const string Focused = "data-focused";
        public const string StartingStyle = "data-starting-style";
        public const string EndingStyle = "data-ending-style";
    }
}
