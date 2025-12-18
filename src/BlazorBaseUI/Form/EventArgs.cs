namespace BlazorBaseUI.Form;

public record FormSubmitEventArgs(IReadOnlyDictionary<string, object?> Values);
