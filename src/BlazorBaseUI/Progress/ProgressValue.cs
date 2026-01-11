using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Progress;

public sealed class ProgressValue : ComponentBase
{
    private const string DefaultTag = "span";

    private bool isComponentRenderAs;

    [CascadingParameter]
    private ProgressRootContext? Context { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<ProgressRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<ProgressRootState, string>? StyleValue { get; set; }

    [Parameter]
    public Func<string, double?, RenderFragment>? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

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
        if (Context is null)
        {
            return;
        }

        var state = Context.State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        var formattedValueArg = !Context.Value.HasValue ? "indeterminate" : Context.FormattedValue;
        var formattedValueDisplay = !Context.Value.HasValue ? null : Context.FormattedValue;

        RenderFragment? content = ChildContent is not null
            ? ChildContent(formattedValueArg, Context.Value)
            : (RenderFragment?)(b => b.AddContent(0, formattedValueDisplay));

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);

        builder.AddAttribute(2, "aria-hidden", "true");

        builder.AddAttribute(3, $"data-{state.Status.ToDataAttributeString()}");

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(4, "class", resolvedClass);
        }
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(5, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddAttribute(6, "ChildContent", content);
            builder.AddComponentReferenceCapture(7, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(6, elementReference => Element = elementReference);
            builder.AddContent(7, content);
            builder.CloseElement();
        }
    }
}
