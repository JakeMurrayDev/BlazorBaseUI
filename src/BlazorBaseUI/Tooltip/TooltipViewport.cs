using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tooltip;

/// <summary>
/// A viewport for displaying content transitions.
/// This component is only required if one popup can be opened by multiple triggers,
/// its content changes based on the trigger, and switching between them is animated.
/// </summary>
public sealed class TooltipViewport : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;
    private IReferencableComponent? componentReference;
    private TooltipViewportState state;

    [CascadingParameter]
    private TooltipRootContext? RootContext { get; set; }

    [CascadingParameter]
    private TooltipPositionerContext? PositionerContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<TooltipViewportState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<TooltipViewportState, string>? StyleValue { get; set; }

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

        var instantType = RootContext?.InstantType ?? TooltipInstantType.None;
        state = new TooltipViewportState(instantType);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RootContext is null)
        {
            return;
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var instantType = RootContext.InstantType;

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);

        var instantAttr = instantType.ToDataAttributeString();
        if (!string.IsNullOrEmpty(instantAttr))
        {
            builder.AddAttribute(2, "data-instant", instantAttr);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(3, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(4, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddAttribute(5, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenElement(0, "div");
                innerBuilder.AddAttribute(1, "data-current", string.Empty);
                innerBuilder.AddContent(2, ChildContent);
                innerBuilder.CloseElement();
            }));
            builder.AddComponentReferenceCapture(6, component =>
            {
                componentReference = (IReferencableComponent)component;
                var newElement = componentReference.Element;
                if (!Nullable.Equals(Element, newElement))
                {
                    Element = newElement;
                }
            });
            builder.CloseComponent();
        }
        else
        {
            builder.OpenElement(7, "div");
            builder.AddAttribute(8, "data-current", string.Empty);
            builder.AddContent(9, ChildContent);
            builder.CloseElement();

            builder.AddElementReferenceCapture(10, elementReference =>
            {
                if (!Nullable.Equals(Element, elementReference))
                {
                    Element = elementReference;
                }
            });
            builder.CloseElement();
        }
    }
}
