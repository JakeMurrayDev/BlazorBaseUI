using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Menu;

public sealed class MenuArrow : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;
    private MenuArrowState state;

    [CascadingParameter]
    private MenuRootContext? RootContext { get; set; }

    [CascadingParameter]
    private MenuPositionerContext? PositionerContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<MenuArrowState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<MenuArrowState, string>? StyleValue { get; set; }

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
        state = new MenuArrowState(open, side, align, uncentered);
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
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            RenderAttributes(builder, resolvedClass, resolvedStyle, open, side, align);
            builder.AddAttribute(11, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(12, component =>
            {
                var newElement = ((IReferencableComponent)component).Element;
                if (!Nullable.Equals(Element, newElement))
                {
                    Element = newElement;
                    PositionerContext?.SetArrowElement(Element);
                }
            });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            RenderAttributes(builder, resolvedClass, resolvedStyle, open, side, align);
            builder.AddContent(11, ChildContent);
            builder.AddElementReferenceCapture(12, elementReference =>
            {
                if (!Nullable.Equals(Element, elementReference))
                {
                    Element = elementReference;
                    PositionerContext?.SetArrowElement(Element);
                }
            });
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    private void RenderAttributes(RenderTreeBuilder builder, string? resolvedClass, string? resolvedStyle, bool open, Side side, Align align)
    {
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

        if (PositionerContext?.AnchorHidden == true)
        {
            builder.AddAttribute(8, "data-anchor-hidden", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(9, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(10, "style", resolvedStyle);
        }
    }
}
