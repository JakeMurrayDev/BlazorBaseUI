using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Diagnostics.CodeAnalysis;

namespace BlazorBaseUI.Separator;

public sealed class Separator : ComponentBase
{
    private const string DefaultTag = "div";

    private ElementReference element;

    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<SeparatorState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<SeparatorState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull]
    public ElementReference? Element => element;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var state = new SeparatorState(Orientation);
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        var attributes = BuildAttributes();
        if (!string.IsNullOrEmpty(resolvedClass))
        {
            attributes["class"] = resolvedClass;
        }
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            attributes["style"] = resolvedStyle;
        }

        if (RenderAs is not null)
        {
            builder.OpenComponent(0, RenderAs);
            builder.AddMultipleAttributes(1, attributes);
            builder.AddAttribute(2, "ChildContent", ChildContent);
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

    private Dictionary<string, object> BuildAttributes()
    {
        var attributes = new Dictionary<string, object>();

        if (AdditionalAttributes is not null)
        {
            foreach (var attr in AdditionalAttributes)
            {
                if (attr.Key is not "class" and not "style")
                {
                    attributes[attr.Key] = attr.Value;
                }
            }
        }

        attributes["role"] = "separator";
        attributes["aria-orientation"] = Orientation.ToDataAttributeString();
        attributes["data-orientation"] = Orientation.ToDataAttributeString();

        return attributes;
    }
}