using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Accordion;

public sealed class AccordionHeader : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "h3";

    private bool isComponentRenderAs;
    private AccordionHeaderState state = new(0, Orientation.Vertical, false, false);

    [CascadingParameter]
    private IAccordionItemContext? ItemContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<AccordionHeaderState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<AccordionHeaderState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        state = new AccordionHeaderState(
            ItemContext?.Index ?? 0,
            ItemContext?.Orientation ?? Orientation.Vertical,
            ItemContext?.Disabled ?? false,
            ItemContext?.Open ?? false);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var currentIndex = ItemContext?.Index ?? 0;
        var currentOrientation = ItemContext?.Orientation ?? Orientation.Vertical;
        var currentDisabled = ItemContext?.Disabled ?? false;
        var currentOpen = ItemContext?.Open ?? false;

        if (state.Index != currentIndex || state.Orientation != currentOrientation || state.Disabled != currentDisabled || state.Open != currentOpen)
        {
            state = state with { Index = currentIndex, Orientation = currentOrientation, Disabled = currentDisabled, Open = currentOpen };
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ItemContext is null)
        {
            return;
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "data-index", state.Index.ToString());
            builder.AddAttribute(3, "data-orientation", state.Orientation.ToDataAttributeString());

            if (state.Open)
            {
                builder.AddAttribute(4, "data-open", string.Empty);
            }
            else
            {
                builder.AddAttribute(5, "data-closed", string.Empty);
            }

            if (state.Disabled)
            {
                builder.AddAttribute(6, "data-disabled", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(7, "class", resolvedClass);
            }
            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(8, "style", resolvedStyle);
            }

            builder.AddAttribute(9, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(10, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "data-index", state.Index.ToString());
            builder.AddAttribute(3, "data-orientation", state.Orientation.ToDataAttributeString());

            if (state.Open)
            {
                builder.AddAttribute(4, "data-open", string.Empty);
            }
            else
            {
                builder.AddAttribute(5, "data-closed", string.Empty);
            }

            if (state.Disabled)
            {
                builder.AddAttribute(6, "data-disabled", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(7, "class", resolvedClass);
            }
            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(8, "style", resolvedStyle);
            }

            builder.AddElementReferenceCapture(9, elementReference => Element = elementReference);
            builder.AddContent(10, ChildContent);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }
}
