using BlazorBaseUI.Field;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorBaseUI.Form;

public interface IFieldRegistration
{
    string? Name { get; }
    Func<object?> GetValue { get; }
    Func<Task> ValidateAsync { get; }
    FieldValidityData ValidityData { get; }
    Func<ValueTask> FocusAsync { get; }
}

public sealed class FieldRegistry
{
    private readonly Dictionary<string, IFieldRegistration> fields = [];

    public IReadOnlyDictionary<string, IFieldRegistration> Fields => fields;

    public void Register(string id, IFieldRegistration registration) =>
        fields[id] = registration;

    public void Unregister(string id) =>
        fields.Remove(id);

    public async Task ValidateAllAsync()
    {
        foreach (var field in fields.Values)
        {
            await field.ValidateAsync();
        }
    }

    public IFieldRegistration? GetFirstInvalid() =>
        fields.Values.FirstOrDefault(f => f.ValidityData.State.Valid == false);
}

public record FormContext(
    EditContext? EditContext,
    Dictionary<string, string[]> Errors,
    Action<string?> ClearErrors,
    ValidationMode ValidationMode,
    Func<bool> GetSubmitAttempted,
    FieldRegistry FieldRegistry)
{
    public static FormContext Default { get; } = new(
        EditContext: null,
        Errors: [],
        ClearErrors: _ => { },
        ValidationMode: ValidationMode.OnSubmit,
        GetSubmitAttempted: () => false,
        FieldRegistry: new FieldRegistry());

    public bool HasError(string? name) =>
        name is not null && Errors.TryGetValue(name, out var errors) && errors.Length > 0;

    public string[] GetErrors(string? name) =>
        name is not null && Errors.TryGetValue(name, out var errors) ? errors : [];
}