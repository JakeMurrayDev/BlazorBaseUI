using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.NumberField;

public sealed class NumberFieldGroup : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;

    [CascadingParameter]
    private INumberFieldRootContext? RootContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<NumberFieldRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<NumberFieldRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private NumberFieldRootState State => RootContext?.State ?? NumberFieldRootState.Default;

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;

        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var state = State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "role", "group");

        if (state.Scrubbing)
        {
            builder.AddAttribute(3, "data-scrubbing", string.Empty);
        }

        if (state.Disabled)
        {
            builder.AddAttribute(4, "data-disabled", string.Empty);
        }

        if (state.ReadOnly)
        {
            builder.AddAttribute(5, "data-readonly", string.Empty);
        }

        if (state.Required)
        {
            builder.AddAttribute(6, "data-required", string.Empty);
        }

        if (state.Valid == true)
        {
            builder.AddAttribute(7, "data-valid", string.Empty);
        }
        else if (state.Valid == false)
        {
            builder.AddAttribute(8, "data-invalid", string.Empty);
        }

        if (state.Touched)
        {
            builder.AddAttribute(9, "data-touched", string.Empty);
        }

        if (state.Dirty)
        {
            builder.AddAttribute(10, "data-dirty", string.Empty);
        }

        if (state.Filled)
        {
            builder.AddAttribute(11, "data-filled", string.Empty);
        }

        if (state.Focused)
        {
            builder.AddAttribute(12, "data-focused", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(13, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(14, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(15, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(16, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(17, elementReference => Element = elementReference);
            builder.AddContent(18, ChildContent);
            builder.CloseElement();
        }
    }
}
