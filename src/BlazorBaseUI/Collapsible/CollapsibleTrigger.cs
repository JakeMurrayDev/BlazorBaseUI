using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Collapsible;

public sealed class CollapsibleTrigger : ComponentBase
{
    private const string DefaultTag = "button";

    private bool isComponentRenderAs;
    private CollapsibleRootState state = new(false, false);
    private EventCallback<MouseEventArgs> cachedClickCallback;

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

    public ElementReference? Element { get; private set; }

    private bool ResolvedDisabled => Disabled ?? Context?.Disabled ?? false;

    protected override void OnInitialized()
    {
        cachedClickCallback = EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var currentOpen = Context?.Open ?? false;
        var currentDisabled = ResolvedDisabled;
        if (state.Open != currentOpen || state.Disabled != currentDisabled)
        {
            state = state with { Open = currentOpen, Disabled = currentDisabled };
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
        {
            return;
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);

        if (NativeButton)
        {
            builder.AddAttribute(2, "type", "button");
        }

        builder.AddAttribute(3, "aria-controls", Context.PanelId);
        builder.AddAttribute(4, "aria-expanded", Context.Open ? "true" : "false");

        if (ResolvedDisabled)
        {
            builder.AddAttribute(5, "disabled", true);
        }

        builder.AddAttribute(6, "onclick", cachedClickCallback);

        if (state.Open)
        {
            builder.AddAttribute(7, "data-panel-open", string.Empty);
        }

        if (state.Disabled)
        {
            builder.AddAttribute(8, "data-disabled", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(9, "class", resolvedClass);
        }
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(10, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddAttribute(11, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(12, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(13, elementReference => Element = elementReference);
            builder.AddContent(14, ChildContent);
            builder.CloseElement();
        }
    }

    private void HandleClick(MouseEventArgs args)
    {
        if (ResolvedDisabled)
        {
            return;
        }

        Context?.HandleTrigger();
    }
}
