using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Form;
using BlazorBaseUI.Utilities.LabelableProvider;

namespace BlazorBaseUI.Field;

public sealed class FieldError : ComponentBase, IFieldStateSubscriber, IDisposable
{
    private const string DefaultTag = "div";

    private string? defaultId;
    private ElementReference element;
    private bool wasRendered;

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

    [DisallowNull]
    public ElementReference? Element => element;

    private FieldRootState State => FieldContext?.State ?? FieldRootState.Default;

    private string? FieldName => FieldContext?.Name;

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    protected override void OnInitialized()
    {
        FieldContext?.SubscribeFunc(this);
    }

    protected override void OnParametersSet()
    {
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

        var attributes = BuildAttributes(state);
        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        if (RenderAs is not null)
        {
            builder.OpenComponent(0, RenderAs);
            builder.AddMultipleAttributes(1, attributes);
            builder.AddComponentParameter(2, "ChildContent", GetErrorContent());
            builder.CloseComponent();
            return;
        }

        var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
        builder.OpenElement(3, tag);
        builder.AddMultipleAttributes(4, attributes);
        builder.AddElementReferenceCapture(5, e => element = e);
        builder.AddContent(6, GetErrorContent());
        builder.CloseElement();
    }

    void IFieldStateSubscriber.NotifyStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
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

    private RenderFragment GetErrorContent()
    {
        if (ChildContent is not null)
            return ChildContent;

        var errors = GetAllErrors();

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
                    builder.AddContent(2, error);
                    builder.CloseElement();
                }
                builder.CloseElement();
            }
        };
    }

    private Dictionary<string, object> BuildAttributes(FieldRootState state)
    {
        var attributes = new Dictionary<string, object>();

        if (AdditionalAttributes is not null)
        {
            foreach (var attr in AdditionalAttributes)
            {
                if (attr.Key is not "class" and not "style")
                    attributes[attr.Key] = attr.Value;
            }
        }

        attributes["id"] = ResolvedId;

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }

    public void Dispose()
    {
        FieldContext?.UnsubscribeFunc(this);
        LabelableContext?.UpdateMessageIds.Invoke(ResolvedId, false);
    }
}