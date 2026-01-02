using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Fieldset;

public sealed class FieldsetRoot : ComponentBase
{
    private const string DefaultTag = "fieldset";

    private string? legendId;
    private ElementReference element;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<FieldsetRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<FieldsetRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull]
    public ElementReference? Element => element;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var state = new FieldsetRootState(Disabled);
        var context = new FieldsetRootContext(
            LegendId: legendId,
            SetLegendId: SetLegendId,
            Disabled: Disabled);

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        var attributes = BuildAttributes(state);
        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        builder.OpenComponent<CascadingValue<FieldsetRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(contentBuilder =>
        {
            if (RenderAs is not null)
            {
                contentBuilder.OpenComponent(4, RenderAs);
                contentBuilder.AddMultipleAttributes(5, attributes);
                contentBuilder.AddComponentParameter(6, "ChildContent", ChildContent);
                contentBuilder.CloseComponent();
            }
            else
            {
                var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
                contentBuilder.OpenElement(7, tag);
                contentBuilder.AddMultipleAttributes(8, attributes);
                contentBuilder.AddElementReferenceCapture(9, e => element = e);
                contentBuilder.AddContent(10, ChildContent);
                contentBuilder.CloseElement();
            }
        }));
        builder.CloseComponent();
    }

    private Dictionary<string, object> BuildAttributes(FieldsetRootState state)
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

        if (!string.IsNullOrEmpty(legendId))
            attributes["aria-labelledby"] = legendId;

        if (Disabled)
            attributes["disabled"] = true;

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }

    private void SetLegendId(string? id)
    {
        if (legendId == id) return;
        legendId = id;
        StateHasChanged();
    }
}
