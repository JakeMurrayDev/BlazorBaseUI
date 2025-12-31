using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Accordion;

public sealed class AccordionHeader : ComponentBase
{
    private const string DefaultTag = "h3";

    private ElementReference element;

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

    [DisallowNull]
    public ElementReference? Element => element;

    private AccordionHeaderState State => new(
        ItemContext?.Index ?? 0,
        ItemContext?.Orientation ?? Orientation.Vertical,
        ItemContext?.Disabled ?? false,
        ItemContext?.Open ?? false);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ItemContext is null)
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
            builder.AddComponentParameter(2, "ChildContent", ChildContent);
            builder.CloseComponent();
            return;
        }

        var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
        builder.OpenElement(3, tag);
        builder.AddMultipleAttributes(4, attributes);
        builder.AddElementReferenceCapture(5, e => element = e);
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
    }

    private Dictionary<string, object> BuildAttributes(AccordionHeaderState state)
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

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }
}

public record AccordionHeaderState(int Index, Orientation Orientation, bool Disabled, bool Open)
{
    public Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>
        {
            [AccordionHeaderDataAttribute.Index.ToDataAttributeString()] = Index.ToString(),
            [AccordionHeaderDataAttribute.Orientation.ToDataAttributeString()] = Orientation.ToDataAttributeString()!
        };

        if (Open)
            attributes[AccordionHeaderDataAttribute.Open.ToDataAttributeString()] = string.Empty;
        else
            attributes[AccordionHeaderDataAttribute.Closed.ToDataAttributeString()] = string.Empty;

        if (Disabled)
            attributes[AccordionHeaderDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}