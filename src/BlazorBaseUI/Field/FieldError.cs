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

    private FieldRootState State => FieldContext?.State ?? FieldRootState.Default;

    private string? FieldName => FieldContext?.Name;

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

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
        var errors = GetAllErrors();

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

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

        if (isComponentRenderAs)
        {
            builder.AddAttribute(12, "ChildContent", ChildContent ?? BuildErrorContent(errors));
            builder.AddComponentReferenceCapture(13, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(14, elementReference => Element = elementReference);

            if (ChildContent is not null)
            {
                builder.AddContent(15, ChildContent);
            }
            else if (errors.Length == 1)
            {
                builder.AddContent(16, errors[0]);
            }
            else if (errors.Length > 1)
            {
                builder.OpenElement(17, "ul");
                foreach (var error in errors)
                {
                    builder.OpenElement(18, "li");
                    builder.AddContent(19, error);
                    builder.CloseElement();
                }
                builder.CloseElement();
            }

            builder.CloseElement();
        }
    }

    void IFieldStateSubscriber.NotifyStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        FieldContext?.Unsubscribe(this);
        LabelableContext?.UpdateMessageIds.Invoke(ResolvedId, false);
    }

    private bool ShouldRenderError()
    {
        if (Match == true)
            return true;

        if (MatchValidity is not null)
            return MatchValidityState(MatchValidity);

        var errors = GetAllErrors();
        return errors.Length > 0;
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

    private string[] GetAllErrors()
    {
        var errors = new List<string>();

        if (EditContext is not null && FieldName is not null)
        {
            var fieldIdentifier = EditContext.Field(FieldName);
            errors.AddRange(EditContext.GetValidationMessages(fieldIdentifier));
        }

        if (FormContext is not null && FieldName is not null)
        {
            errors.AddRange(FormContext.GetErrors(FieldName));
        }

        if (FieldContext?.ValidityData.Errors is { Length: > 0 } validityErrors)
        {
            errors.AddRange(validityErrors);
        }

        return [.. errors.Distinct()];
    }

    private RenderFragment BuildErrorContent(string[] errors)
    {
        return builder =>
        {
            if (errors.Length == 1)
            {
                builder.AddContent(0, errors[0]);
            }
            else if (errors.Length > 1)
            {
                builder.OpenElement(1, "ul");
                foreach (var error in errors)
                {
                    builder.OpenElement(2, "li");
                    builder.AddContent(3, error);
                    builder.CloseElement();
                }
                builder.CloseElement();
            }
        };
    }
}
