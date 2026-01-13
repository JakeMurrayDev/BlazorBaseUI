using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorBaseUI.NumberField;

public sealed class NumberFieldScrubAreaCursor : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "span";

    private bool isComponentRenderAs;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private INumberFieldRootContext? RootContext { get; set; }

    [CascadingParameter]
    private INumberFieldScrubAreaContext? ScrubAreaContext { get; set; }

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

    private bool ShouldRenderCursor =>
        ScrubAreaContext?.IsScrubbing == true &&
        !ScrubAreaContext.IsTouchInput &&
        !ScrubAreaContext.IsPointerLockDenied;

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;

        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (Element.HasValue && ShouldRenderCursor)
        {
            ScrubAreaContext?.SetCursorElement(Element);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!ShouldRenderCursor)
            return;

        var state = State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var baseStyle = "position:fixed;top:0;left:0;pointer-events:none;";

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "role", "presentation");
        builder.AddAttribute(3, "style", baseStyle + (resolvedStyle ?? string.Empty));

        if (state.Scrubbing)
        {
            builder.AddAttribute(4, "data-scrubbing", string.Empty);
        }

        if (state.Disabled)
        {
            builder.AddAttribute(5, "data-disabled", string.Empty);
        }

        if (state.ReadOnly)
        {
            builder.AddAttribute(6, "data-readonly", string.Empty);
        }

        if (state.Required)
        {
            builder.AddAttribute(7, "data-required", string.Empty);
        }

        if (state.Valid == true)
        {
            builder.AddAttribute(8, "data-valid", string.Empty);
        }
        else if (state.Valid == false)
        {
            builder.AddAttribute(9, "data-invalid", string.Empty);
        }

        if (state.Touched)
        {
            builder.AddAttribute(10, "data-touched", string.Empty);
        }

        if (state.Dirty)
        {
            builder.AddAttribute(11, "data-dirty", string.Empty);
        }

        if (state.Filled)
        {
            builder.AddAttribute(12, "data-filled", string.Empty);
        }

        if (state.Focused)
        {
            builder.AddAttribute(13, "data-focused", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(14, "class", resolvedClass);
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
