using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Popover;

public sealed class PopoverArrow : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;
    private IReferencableComponent? componentReference;
    private PopoverArrowState state;

    [CascadingParameter]
    private PopoverRootContext? RootContext { get; set; }

    [CascadingParameter]
    private PopoverPositionerContext? PositionerContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<PopoverArrowState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<PopoverArrowState, string>? StyleValue { get; set; }

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
        var side = PositionerContext?.Side ?? Side.Bottom;
        var align = PositionerContext?.Align ?? Align.Center;
        var uncentered = PositionerContext?.ArrowUncentered ?? false;
        state = new PopoverArrowState(open, side, align, uncentered);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RootContext is null || PositionerContext is null)
        {
            return;
        }

        var open = RootContext.GetOpen();
        var side = PositionerContext.Side;
        var align = PositionerContext.Align;
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
        builder.AddAttribute(2, "aria-hidden", "true");
        builder.AddAttribute(3, "data-side", side.ToDataAttributeString());
        builder.AddAttribute(4, "data-align", align.ToDataAttributeString());

        if (open)
        {
            builder.AddAttribute(5, "data-open", string.Empty);
        }
        else
        {
            builder.AddAttribute(6, "data-closed", string.Empty);
        }

        if (state.Uncentered)
        {
            builder.AddAttribute(7, "data-uncentered", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(8, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(9, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddAttribute(10, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(11, component =>
            {
                componentReference = (IReferencableComponent)component;
                var newElement = componentReference.Element;
                if (!Nullable.Equals(Element, newElement))
                {
                    Element = newElement;
                    PositionerContext?.SetArrowElement(Element);
                }
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddContent(12, ChildContent);
            builder.AddElementReferenceCapture(13, elementReference =>
            {
                if (!Nullable.Equals(Element, elementReference))
                {
                    Element = elementReference;
                    PositionerContext?.SetArrowElement(Element);
                }
            });
            builder.CloseElement();
        }
    }
}
