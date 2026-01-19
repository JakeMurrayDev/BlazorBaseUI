using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Popover;

public sealed class PopoverViewport : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;
    private PopoverViewportState state;

    [CascadingParameter]
    private PopoverRootContext? RootContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<PopoverViewportState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<PopoverViewportState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

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

        var open = RootContext?.GetOpen() ?? false;
        var transitionStatus = RootContext?.TransitionStatus ?? TransitionStatus.None;
        state = new PopoverViewportState(open, transitionStatus);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RootContext is null)
        {
            return;
        }

        var open = RootContext.GetOpen();
        var transitionStatus = RootContext.TransitionStatus;
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

        if (open)
        {
            builder.AddAttribute(2, "data-open", string.Empty);
        }
        else
        {
            builder.AddAttribute(3, "data-closed", string.Empty);
        }

        if (transitionStatus == TransitionStatus.Starting)
        {
            builder.AddAttribute(4, "data-starting-style", string.Empty);
        }
        else if (transitionStatus == TransitionStatus.Ending)
        {
            builder.AddAttribute(5, "data-ending-style", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(6, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(7, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddAttribute(8, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(9, component =>
            {
                Element = ((IReferencableComponent)component).Element;
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddContent(10, ChildContent);
            builder.AddElementReferenceCapture(11, elementReference => Element = elementReference);
            builder.CloseElement();
        }
    }
}
