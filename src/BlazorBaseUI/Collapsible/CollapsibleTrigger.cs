using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Collapsible;

public sealed class CollapsibleTrigger : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "button";

    private bool isComponentRenderAs;
    private CollapsibleRootState state = new(false, false, TransitionStatus.Undefined);

    private bool ResolvedDisabled => Disabled ?? Context?.Disabled ?? false;

    private bool IsNativeButton => NativeButton && string.IsNullOrEmpty(As) && RenderAs is null;

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

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var currentOpen = Context?.Open ?? false;
        var currentDisabled = ResolvedDisabled;
        var currentTransitionStatus = Context?.TransitionStatus ?? TransitionStatus.Undefined;
        if (state.Open != currentOpen || state.Disabled != currentDisabled || state.TransitionStatus != currentTransitionStatus)
        {
            state = state with { Open = currentOpen, Disabled = currentDisabled, TransitionStatus = currentTransitionStatus };
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
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            RenderCommonAttributes(builder);
            RenderDataAttributes(builder);
            RenderClassAndStyle(builder, resolvedClass, resolvedStyle);
            builder.AddAttribute(14, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(15, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            RenderCommonAttributes(builder);
            RenderDataAttributes(builder);
            RenderClassAndStyle(builder, resolvedClass, resolvedStyle);
            builder.AddElementReferenceCapture(14, elementReference => Element = elementReference);
            builder.AddContent(15, ChildContent);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    private void RenderCommonAttributes(RenderTreeBuilder builder)
    {
        if (IsNativeButton)
        {
            builder.AddAttribute(2, "type", "button");
        }
        else
        {
            builder.AddAttribute(3, "role", "button");
        }

        if (Context!.Open)
        {
            builder.AddAttribute(4, "aria-controls", Context.PanelId);
        }

        builder.AddAttribute(5, "aria-expanded", Context.Open ? "true" : "false");

        if (ResolvedDisabled)
        {
            builder.AddAttribute(6, "disabled", true);
        }

        builder.AddAttribute(7, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));
    }

    private void RenderDataAttributes(RenderTreeBuilder builder)
    {
        if (state.Open)
        {
            builder.AddAttribute(8, "data-panel-open", string.Empty);
        }

        if (state.Disabled)
        {
            builder.AddAttribute(9, "data-disabled", string.Empty);
        }

        if (state.TransitionStatus == TransitionStatus.Starting)
        {
            builder.AddAttribute(10, "data-starting-style", string.Empty);
        }

        if (state.TransitionStatus == TransitionStatus.Ending)
        {
            builder.AddAttribute(11, "data-ending-style", string.Empty);
        }
    }

    private void RenderClassAndStyle(RenderTreeBuilder builder, string? resolvedClass, string? resolvedStyle)
    {
        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(12, "class", resolvedClass);
        }
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(13, "style", resolvedStyle);
        }
    }

    private async Task HandleClickAsync(MouseEventArgs args)
    {
        if (ResolvedDisabled)
        {
            return;
        }

        Context?.HandleTrigger();
        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, args);
    }
}
