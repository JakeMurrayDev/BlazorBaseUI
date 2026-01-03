namespace BlazorBaseUI.Field;

public sealed record FieldValidityData(
    FieldValidityState State,
    string[] Errors,
    string Error,
    object? Value,
    object? InitialValue)
{
    public static FieldValidityData Default { get; } = new(
        State: FieldValidityState.Default,
        Errors: [],
        Error: string.Empty,
        Value: null,
        InitialValue: null);
}
