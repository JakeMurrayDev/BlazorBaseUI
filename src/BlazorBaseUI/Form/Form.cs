using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Form;

public sealed class Form : ComponentBase
{
    private const string DefaultTag = "form";

    private readonly FieldRegistry fieldRegistry = new();

    private EditContext? editContext;
    private bool hasSetEditContextExplicitly;
    private bool submitAttempted;
    private Dictionary<string, string[]> errors = new(4);
    private Dictionary<string, string[]>? previousExternalErrors;
    private FormContext formContext = null!;
    private Dictionary<string, object>? cachedAttributes;
    private EventCallback<EventArgs> cachedSubmitCallback;

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
        cachedSubmitCallback = EventCallback.Factory.Create<EventArgs>(this, HandleSubmitAsync);

        formContext = new FormContext(
            editContext: editContext,
            fieldRegistry: fieldRegistry,
            clearErrors: ClearErrors,
            getSubmitAttempted: () => submitAttempted);
    }

    protected override void OnParametersSet()
    {
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
        cachedAttributes = null;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        Debug.Assert(editContext is not null);

        var state = FormState.Default;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        builder.OpenRegion(editContext.GetHashCode());

        var attributes = GetOrBuildAttributes();

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        else
            attributes.Remove("class");

        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;
        else
            attributes.Remove("style");

        if (RenderAs is not null)
        {
            if (!typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
            {
                throw new InvalidOperationException($"Type {RenderAs.Name} must implement IReferencableComponent.");
            }
            builder.OpenComponent(0, RenderAs);
            builder.AddMultipleAttributes(1, attributes);
            builder.AddComponentParameter(2, "ChildContent", BuildChildContent());
            builder.AddComponentReferenceCapture(3, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
            builder.OpenElement(3, tag);
            builder.AddMultipleAttributes(4, attributes);
            builder.AddElementReferenceCapture(5, e => Element = e);
            builder.AddContent(6, BuildChildContent());
            builder.CloseElement();
        }

        builder.CloseRegion();
    }

    private RenderFragment BuildChildContent() => builder =>
    {
        Debug.Assert(editContext is not null);

        builder.OpenComponent<CascadingValue<EditContext>>(0);
        builder.AddComponentParameter(1, "IsFixed", true);
        builder.AddComponentParameter(2, "Value", editContext);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(editContextBuilder =>
        {
            editContextBuilder.OpenComponent<CascadingValue<FormContext>>(4);
            editContextBuilder.AddComponentParameter(5, "Value", formContext);
            editContextBuilder.AddComponentParameter(6, "IsFixed", true);
            editContextBuilder.AddComponentParameter(7, "ChildContent", ChildContent?.Invoke(editContext));
            editContextBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    };

    private void UpdateContext()
    {
        formContext.Update(
            editContext: editContext,
            errors: errors,
            validationMode: ValidationMode);
    }

    private Dictionary<string, object> GetOrBuildAttributes()
    {
        if (cachedAttributes is not null)
            return cachedAttributes;

        cachedAttributes = BuildAttributes();
        return cachedAttributes;
    }

    private Dictionary<string, object> BuildAttributes()
    {
        var additionalCount = AdditionalAttributes?.Count ?? 0;
        var attributes = new Dictionary<string, object>(additionalCount + 2);

        if (AdditionalAttributes is not null)
        {
            foreach (var attr in AdditionalAttributes)
            {
                if (attr.Key is not "class" and not "style")
                    attributes[attr.Key] = attr.Value;
            }
        }

        attributes["novalidate"] = true;
        attributes["onsubmit"] = cachedSubmitCallback;

        return attributes;
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
