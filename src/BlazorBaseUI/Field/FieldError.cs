using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Form;
using BlazorBaseUI.Utilities.LabelableProvider;

namespace BlazorBaseUI.Field;

public sealed class FieldError : ComponentBase, IReferencableComponent, IFieldStateSubscriber, IDisposable
{
    private const string DefaultTag = "div";

    private string? defaultId;
    private bool wasRendered;
    private bool isComponentRenderAs;

    private FieldRootState State => FieldContext?.State ?? FieldRootState.Default;

    private string? FieldName => FieldContext?.Name;

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    [CascadingParameter]
    private LabelableContext? LabelableContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    [CascadingParameter]
    private FormContext? FormContext { get; set; }

    [Parameter]
    public bool? Match { get; set; }

    [Parameter]
    public string? MatchValidity { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<FieldRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<FieldRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        FieldContext?.Subscribe(this);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var shouldRender = ShouldRenderError();

        if (shouldRender != wasRendered)
        {
            if (shouldRender)
                LabelableContext?.UpdateMessageIds.Invoke(ResolvedId, true);
            else
                LabelableContext?.UpdateMessageIds.Invoke(ResolvedId, false);

            wasRendered = shouldRender;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!ShouldRenderError())
            return;

        var state = State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var errorContent = GetErrorContent();

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "id", ResolvedId);

            if (state.Disabled)
            {
                builder.AddAttribute(3, "data-disabled", string.Empty);
            }

            if (state.Valid == true)
            {
                builder.AddAttribute(4, "data-valid", string.Empty);
            }
            else if (state.Valid == false)
            {
                builder.AddAttribute(5, "data-invalid", string.Empty);
            }

            if (state.Touched)
            {
                builder.AddAttribute(6, "data-touched", string.Empty);
            }

            if (state.Dirty)
            {
                builder.AddAttribute(7, "data-dirty", string.Empty);
            }

            if (state.Filled)
            {
                builder.AddAttribute(8, "data-filled", string.Empty);
            }

            if (state.Focused)
            {
                builder.AddAttribute(9, "data-focused", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(10, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(11, "style", resolvedStyle);
            }

            builder.AddAttribute(12, "ChildContent", ChildContent ?? BuildErrorContent(errorContent));
            builder.AddComponentReferenceCapture(13, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "id", ResolvedId);

            if (state.Disabled)
            {
                builder.AddAttribute(3, "data-disabled", string.Empty);
            }

            if (state.Valid == true)
            {
                builder.AddAttribute(4, "data-valid", string.Empty);
            }
            else if (state.Valid == false)
            {
                builder.AddAttribute(5, "data-invalid", string.Empty);
            }

            if (state.Touched)
            {
                builder.AddAttribute(6, "data-touched", string.Empty);
            }

            if (state.Dirty)
            {
                builder.AddAttribute(7, "data-dirty", string.Empty);
            }

            if (state.Filled)
            {
                builder.AddAttribute(8, "data-filled", string.Empty);
            }

            if (state.Focused)
            {
                builder.AddAttribute(9, "data-focused", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(10, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(11, "style", resolvedStyle);
            }

            builder.AddElementReferenceCapture(12, elementReference => Element = elementReference);

            if (ChildContent is not null)
            {
                builder.AddContent(13, ChildContent);
            }
            else if (errorContent.formError is not null)
            {
                builder.AddContent(14, errorContent.formError);
            }
            else if (errorContent.validityErrors.Length > 1)
            {
                builder.OpenElement(15, "ul");
                foreach (var error in errorContent.validityErrors)
                {
                    builder.OpenElement(16, "li");
                    builder.AddContent(17, error);
                    builder.CloseElement();
                }
                builder.CloseElement();
            }
            else if (!string.IsNullOrEmpty(errorContent.validityError))
            {
                builder.AddContent(18, errorContent.validityError);
            }

            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    public void Dispose()
    {
        FieldContext?.Unsubscribe(this);
        if (wasRendered)
            LabelableContext?.UpdateMessageIds.Invoke(ResolvedId, false);
    }

    void IFieldStateSubscriber.NotifyStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private bool ShouldRenderError()
    {
        var formError = GetFormError();
        if (formError is not null || Match == true)
            return true;

        if (MatchValidity is not null)
            return MatchValidityState(MatchValidity);

        var validityData = FieldContext?.ValidityData ?? FieldValidityData.Default;
        return validityData.State.Valid == false;
    }

    private bool MatchValidityState(string validity)
    {
        var validityData = FieldContext?.ValidityData ?? FieldValidityData.Default;
        return validity.ToLowerInvariant() switch
        {
            "badinput" => validityData.State.BadInput,
            "customerror" => validityData.State.CustomError,
            "patternmismatch" => validityData.State.PatternMismatch,
            "rangeoverflow" => validityData.State.RangeOverflow,
            "rangeunderflow" => validityData.State.RangeUnderflow,
            "stepmismatch" => validityData.State.StepMismatch,
            "toolong" => validityData.State.TooLong,
            "tooshort" => validityData.State.TooShort,
            "typemismatch" => validityData.State.TypeMismatch,
            "valuemissing" => validityData.State.ValueMissing,
            _ => false
        };
    }

    private string? GetFormError()
    {
        if (FieldName is null)
            return null;

        if (FormContext is not null)
        {
            var formErrors = FormContext.GetErrors(FieldName);
            if (formErrors.Length > 0)
                return formErrors[0];
        }

        if (EditContext is not null)
        {
            var fieldIdentifier = EditContext.Field(FieldName);
            var editContextErrors = EditContext.GetValidationMessages(fieldIdentifier).ToArray();
            if (editContextErrors.Length > 0)
                return editContextErrors[0];
        }

        return null;
    }

    private (string? formError, string[] validityErrors, string? validityError) GetErrorContent()
    {
        var formError = GetFormError();
        var validityData = FieldContext?.ValidityData ?? FieldValidityData.Default;
        return (formError, validityData.Errors, validityData.Error);
    }

    private RenderFragment BuildErrorContent((string? formError, string[] validityErrors, string? validityError) errorContent)
    {
        return builder =>
        {
            if (errorContent.formError is not null)
            {
                builder.AddContent(0, errorContent.formError);
            }
            else if (errorContent.validityErrors.Length > 1)
            {
                builder.OpenElement(1, "ul");
                foreach (var error in errorContent.validityErrors)
                {
                    builder.OpenElement(2, "li");
                    builder.AddContent(3, error);
                    builder.CloseElement();
                }
                builder.CloseElement();
            }
            else if (!string.IsNullOrEmpty(errorContent.validityError))
            {
                builder.AddContent(4, errorContent.validityError);
            }
        };
    }
}
