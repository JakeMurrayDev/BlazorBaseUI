using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Form;

public sealed class FormActions
{
    private readonly Func<string?, Task> validateAsync;

    internal FormActions(Func<string?, Task> validateAsync)
    {
        this.validateAsync = validateAsync;
    }

    public Task ValidateAsync() => validateAsync(null);

    public Task ValidateAsync(string fieldName) => validateAsync(fieldName);
}

public sealed class Form : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "form";

    private readonly FieldRegistry fieldRegistry = new();

    private EditContext? editContext;
    private bool hasSetEditContextExplicitly;
    private bool submitAttempted;
    private bool isComponentRenderAs;
    private Dictionary<string, string[]> errors = new(4);
    private Dictionary<string, string[]>? previousExternalErrors;
    private FormContext formContext = null!;
    private FormActions? actions;

    [Parameter]
#pragma warning disable BL0007
    public EditContext? EditContext
#pragma warning restore BL0007
    {
        get => editContext;
        set
        {
            editContext = value;
            hasSetEditContextExplicitly = value is not null;
        }
    }

    [Parameter]
    public object? Model { get; set; }

    [Parameter]
    public ValidationMode ValidationMode { get; set; } = ValidationMode.OnSubmit;

    [Parameter]
    public Dictionary<string, string[]>? Errors { get; set; }

    [Parameter]
    public EventCallback<EditContext> OnSubmit { get; set; }

    [Parameter]
    public EventCallback<EditContext> OnValidSubmit { get; set; }

    [Parameter]
    public EventCallback<EditContext> OnInvalidSubmit { get; set; }

    [Parameter]
    public EventCallback<FormSubmitEventArgs> OnFormSubmit { get; set; }

    [Parameter]
    public Action<FormActions>? ActionsRef { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<FormState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<FormState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment<EditContext>? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        formContext = new FormContext(
            editContext: editContext,
            fieldRegistry: fieldRegistry,
            clearErrors: ClearErrors,
            getSubmitAttempted: () => submitAttempted);

        actions = new FormActions(ImperativeValidateAsync);
        ActionsRef?.Invoke(actions);
    }

    private async Task ImperativeValidateAsync(string? fieldName)
    {
        if (fieldName is not null)
        {
            var field = fieldRegistry.Fields.Values.FirstOrDefault(f => f.Name == fieldName);
            if (field is not null)
            {
                await field.ValidateAsync();
            }
        }
        else
        {
            await fieldRegistry.ValidateAllAsync();
        }
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (hasSetEditContextExplicitly && Model is not null)
        {
            throw new InvalidOperationException(
                $"{nameof(Form)} requires a {nameof(Model)} parameter, or an {nameof(EditContext)} parameter, but not both.");
        }

        if (!hasSetEditContextExplicitly && Model is null)
        {
            throw new InvalidOperationException(
                $"{nameof(Form)} requires either a {nameof(Model)} parameter, or an {nameof(EditContext)} parameter.");
        }

        if (OnSubmit.HasDelegate && (OnValidSubmit.HasDelegate || OnInvalidSubmit.HasDelegate))
        {
            throw new InvalidOperationException(
                $"When supplying an {nameof(OnSubmit)} parameter to {nameof(Form)}, do not also supply {nameof(OnValidSubmit)} or {nameof(OnInvalidSubmit)}.");
        }

        if (Model is not null && Model != editContext?.Model)
        {
            editContext = new EditContext(Model);
        }

        if (!ReferenceEquals(Errors, previousExternalErrors))
        {
            previousExternalErrors = Errors;
            errors = Errors is not null
                ? new Dictionary<string, string[]>(Errors)
                : new Dictionary<string, string[]>(4);
        }

        UpdateContext();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        Debug.Assert(editContext is not null);

        var state = FormState.Default;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "novalidate", true);
            builder.AddAttribute(3, "onsubmit", EventCallback.Factory.Create<EventArgs>(this, HandleSubmitAsync));

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(4, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(5, "style", resolvedStyle);
            }

            builder.AddAttribute(6, "ChildContent", (RenderFragment)RenderChildContent);
            builder.AddComponentReferenceCapture(7, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "novalidate", true);
            builder.AddAttribute(3, "onsubmit", EventCallback.Factory.Create<EventArgs>(this, HandleSubmitAsync));

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(4, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(5, "style", resolvedStyle);
            }

            builder.AddElementReferenceCapture(6, e => Element = e);
            builder.AddContent(7, (RenderFragment)RenderChildContent);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    private void RenderChildContent(RenderTreeBuilder builder)
    {
        Debug.Assert(editContext is not null);

        builder.OpenComponent<CascadingValue<EditContext>>(0);
        builder.AddComponentParameter(1, "Value", editContext);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)RenderEditContextContent);
        builder.CloseComponent();
    }

    private void RenderEditContextContent(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<FormContext>>(0);
        builder.AddComponentParameter(1, "Value", formContext);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)RenderFormContextContent);
        builder.CloseComponent();
    }

    private void RenderFormContextContent(RenderTreeBuilder builder)
    {
        Debug.Assert(editContext is not null);
        builder.AddContent(0, ChildContent?.Invoke(editContext));
    }

    private void UpdateContext()
    {
        formContext.Update(
            editContext: editContext,
            errors: errors,
            validationMode: ValidationMode);
    }

    private void ClearErrors(string? name)
    {
        if (name is not null && errors.Remove(name))
        {
            UpdateContext();
            _ = InvokeAsync(StateHasChanged);
        }
    }

    private async Task HandleSubmitAsync()
    {
        Debug.Assert(editContext is not null);

        submitAttempted = true;
        UpdateContext();

        if (OnSubmit.HasDelegate)
        {
            await OnSubmit.InvokeAsync(editContext);
            return;
        }

        await fieldRegistry.ValidateAllAsync();

        var isValid = editContext.Validate();

        var firstInvalidField = fieldRegistry.GetFirstInvalid();
        if (firstInvalidField is not null)
        {
            isValid = false;
            await firstInvalidField.FocusAsync();
        }

        if (isValid && OnValidSubmit.HasDelegate)
        {
            await OnValidSubmit.InvokeAsync(editContext);
        }

        if (!isValid && OnInvalidSubmit.HasDelegate)
        {
            await OnInvalidSubmit.InvokeAsync(editContext);
        }

        if (isValid && OnFormSubmit.HasDelegate)
        {
            var fieldCount = fieldRegistry.Fields.Count;
            var formValues = new Dictionary<string, object?>(fieldCount);
            foreach (var (_, field) in fieldRegistry.Fields)
            {
                if (field.Name is not null)
                {
                    formValues[field.Name] = field.GetValue();
                }
            }

            await OnFormSubmit.InvokeAsync(new FormSubmitEventArgs(formValues));
        }
    }
}
