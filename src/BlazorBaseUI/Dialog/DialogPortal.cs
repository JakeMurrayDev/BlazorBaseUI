using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Dialog;

public sealed class DialogPortal : ComponentBase, IReferencableComponent
{
    private IReferencableComponent? portalReference;

    [CascadingParameter]
    private DialogRootContext? Context { get; set; }

    [Parameter]
    public bool KeepMounted { get; set; }

    [Parameter]
    public string Container { get; set; } = "body";

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        if (Context is null)
        {
            throw new InvalidOperationException("DialogPortal must be used within a DialogRoot.");
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
        {
            return;
        }

        var shouldRender = KeepMounted || Context.Mounted;

        if (!shouldRender)
        {
            return;
        }

        builder.OpenComponent<CascadingValue<DialogPortalContext>>(0);
        builder.AddComponentParameter(1, "Value", new DialogPortalContext(KeepMounted));
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)RenderPortal);
        builder.CloseComponent();
    }

    private void RenderPortal(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Portal.Portal>(0);
        builder.AddAttribute(1, "Target", Container);
        builder.AddAttribute(2, "ChildContent", ChildContent);
        builder.AddComponentReferenceCapture(3, component =>
        {
            portalReference = (IReferencableComponent)component;
            Element = portalReference.Element;
        });
        builder.CloseComponent();
    }
}

internal sealed record DialogPortalContext(bool KeepMounted);
