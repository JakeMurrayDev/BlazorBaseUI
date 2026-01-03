using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Fieldset;

public sealed class FieldsetLegend : ComponentBase, IDisposable
{
    private const string DefaultTag = "div";

    private string? defaultId;

    [CascadingParameter]
    private FieldsetRootContext? FieldsetContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<FieldsetLegendState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<FieldsetLegendState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    private bool Disabled => FieldsetContext?.Disabled ?? false;

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    protected override void OnInitialized()
    {
        FieldsetContext?.SetLegendId(ResolvedId);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var state = new FieldsetLegendState(Disabled);
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
        builder.AddElementReferenceCapture(5, e => Element = e);
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
    }

    public void Dispose()
    {
        FieldsetContext?.SetLegendId(null);
    }

    private Dictionary<string, object> BuildAttributes(FieldsetLegendState state)
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

        attributes["id"] = ResolvedId;

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }
}
