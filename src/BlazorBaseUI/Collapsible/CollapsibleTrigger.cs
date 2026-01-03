using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Collapsible;

public sealed class CollapsibleTrigger : ComponentBase
{
    private const string DefaultTag = "button";

    [CascadingParameter]
    private CollapsibleRootContext? Context { get; set; }

    [Parameter]
    public bool? Disabled { get; set; }

    [Parameter]
    public bool NativeButton { get; set; } = true;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<CollapsibleRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<CollapsibleRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    private bool ResolvedDisabled => Disabled ?? Context?.Disabled ?? false;

    private CollapsibleRootState State => new(
        Context?.Open ?? false,
        ResolvedDisabled,
        TransitionStatus.Undefined);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
            return;
        
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

    private Dictionary<string, object> BuildAttributes(CollapsibleRootState state)
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

        if (NativeButton)
            attributes["type"] = "button";

        if (Context!.Open)
            attributes["aria-controls"] = Context.PanelId;

        attributes["aria-expanded"] = Context.Open;

        if (ResolvedDisabled)
            attributes["Disabled"] = true;

        attributes["onclick"] = EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick);

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }

    private void HandleClick(MouseEventArgs args)
    {
        if (ResolvedDisabled)
            return;

        Context?.HandleTrigger();
    }
}