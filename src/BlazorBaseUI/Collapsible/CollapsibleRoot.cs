using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Collapsible;

public sealed class CollapsibleRoot : ComponentBase
{
    private const string DefaultTag = "div";

    private bool isOpen;
    private string panelId = null!;
    private CollapsibleRootContext context = null!;
    private ElementReference element;

    [Parameter]
    public bool? Open { get; set; }

    [Parameter]
    public bool DefaultOpen { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public EventCallback<CollapsibleOpenChangeEventArgs> OnOpenChange { get; set; }

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
    public ElementReference? Element => element;

    private bool IsControlled => Open.HasValue;

    private bool CurrentOpen => IsControlled ? Open!.Value : isOpen;

    protected override void OnInitialized()
    {
        panelId = Guid.NewGuid().ToIdString();
        isOpen = DefaultOpen;
        context = CreateContext();
    }

    protected override void OnParametersSet()
    {
        context = CreateContext();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var state = context.State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var attributes = BuildAttributes(state);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        builder.OpenComponent<CascadingValue<CollapsibleRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(innerBuilder =>
        {
            if (RenderAs is not null)
            {
                innerBuilder.OpenComponent(4, RenderAs);
                innerBuilder.AddMultipleAttributes(5, attributes);
                innerBuilder.AddComponentParameter(6, "ChildContent", ChildContent);
                innerBuilder.CloseComponent();
            }
            else
            {
                var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
                innerBuilder.OpenElement(7, tag);
                innerBuilder.AddMultipleAttributes(8, attributes);
                innerBuilder.AddElementReferenceCapture(9, e => element = e);
                innerBuilder.AddContent(10, ChildContent);
                innerBuilder.CloseElement();
            }
        }));
        builder.CloseComponent();
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

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }

    private CollapsibleRootContext CreateContext() => new(
        Open: CurrentOpen,
        Disabled: Disabled,
        PanelId: panelId,
        HandleTrigger: HandleTrigger);

    private void HandleTrigger()
    {
        if (Disabled)
            return;

        var nextOpen = !CurrentOpen;
        var args = new CollapsibleOpenChangeEventArgs(nextOpen);

        OnOpenChange.InvokeAsync(args);

        if (args.Canceled)
            return;

        if (!IsControlled)
        {
            isOpen = nextOpen;
        }

        OpenChanged.InvokeAsync(nextOpen);
        context = CreateContext();
        StateHasChanged();
    }
}