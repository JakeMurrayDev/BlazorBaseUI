namespace BlazorBaseUI.Form;

public sealed record FormSubmitEventArgs(IReadOnlyDictionary<string, object?> Values);
