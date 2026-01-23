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
    private readonly Dictionary<string, IFieldRegistration> fields = new(8);

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

public interface IFormContext
{
    EditContext? EditContext { get; }
    ValidationMode ValidationMode { get; }
    FieldRegistry FieldRegistry { get; }

    bool HasError(string? name);
    string[] GetErrors(string? name);
    void ClearErrors(string? name);
    bool GetSubmitAttempted();
}

public sealed class FormContext : IFormContext
{
    private Dictionary<string, string[]> errors = new(4);
    private Action<string?>? clearErrorsCallback;
    private Func<bool>? getSubmitAttemptedCallback;

    public EditContext? EditContext { get; private set; }
    public ValidationMode ValidationMode { get; private set; } = ValidationMode.OnSubmit;
    public FieldRegistry FieldRegistry { get; private set; } = new();

    private FormContext() { }

    public FormContext(
        EditContext? editContext,
        FieldRegistry fieldRegistry,
        Action<string?> clearErrors,
        Func<bool> getSubmitAttempted)
    {
        EditContext = editContext;
        FieldRegistry = fieldRegistry;
        clearErrorsCallback = clearErrors;
        getSubmitAttemptedCallback = getSubmitAttempted;
    }

    internal void Update(
        EditContext? editContext,
        Dictionary<string, string[]> errors,
        ValidationMode validationMode)
    {
        EditContext = editContext;
        this.errors = errors;
        ValidationMode = validationMode;
    }

    public bool HasError(string? name) =>
        name is not null && errors.TryGetValue(name, out var errorList) && errorList.Length > 0;

    public string[] GetErrors(string? name) =>
        name is not null && errors.TryGetValue(name, out var errorList) ? errorList : [];

    public void ClearErrors(string? name) => clearErrorsCallback?.Invoke(name);

    public bool GetSubmitAttempted() => getSubmitAttemptedCallback?.Invoke() ?? false;
}
