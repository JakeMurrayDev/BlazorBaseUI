using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Dialog;

public sealed class DialogPortal : ComponentBase
{
    private bool previousMounted;

    [CascadingParameter]
    private DialogRootContext? Context { get; set; }

    [Parameter]
    public bool KeepMounted { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnInitialized()
    {
        if (Context is null)
        {
            throw new InvalidOperationException("DialogPortal must be used within a DialogRoot.");
        }

        previousMounted = Context.Mounted;
    }

    protected override void OnParametersSet()
    {
        if (Context is not null)
        {
            previousMounted = Context.Mounted;
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
        builder.AddAttribute(1, "ChildContent", ChildContent);
        builder.CloseComponent();
    }
}

internal sealed record DialogPortalContext(bool KeepMounted);
