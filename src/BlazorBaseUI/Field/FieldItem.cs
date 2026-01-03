using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Utilities.LabelableProvider;
using BlazorBaseUI.CheckboxGroup;

namespace BlazorBaseUI.Field;

public sealed class FieldItem : ComponentBase
{
    private const string DefaultTag = "div";

    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    [CascadingParameter]
    private CheckboxGroupContext? CheckboxGroupContext { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<FieldRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<FieldRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    private bool ResolvedDisabled => (FieldContext?.Disabled ?? false) || Disabled;

    private FieldRootState State => FieldContext?.State ?? FieldRootState.Default;

    private bool HasParentCheckbox => CheckboxGroupContext?.AllValues is not null;

    private string? InitialControlId => HasParentCheckbox ? CheckboxGroupContext?.Parent?.Id : null;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        
        var itemContext = new FieldItemContext(ResolvedDisabled);

        builder.OpenComponent<LabelableProvider>(0);
        builder.AddComponentParameter(1, "InitialControlId", InitialControlId);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(labelableBuilder =>
        {
            labelableBuilder.OpenComponent<CascadingValue<FieldItemContext>>(3);
            labelableBuilder.AddComponentParameter(4, "Value", itemContext);
            labelableBuilder.AddComponentParameter(5, "IsFixed", false);
            labelableBuilder.AddComponentParameter(6, "ChildContent", (RenderFragment)(RenderItem));
            labelableBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private void RenderItem(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(State));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(State));
        var attributes = BuildAttributes(State);

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

    private Dictionary<string, object> BuildAttributes(FieldRootState state)
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