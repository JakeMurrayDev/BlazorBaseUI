using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Popover;

public sealed class PopoverPortal : ComponentBase
{
    private PopoverPortalContext portalContext = null!;

    [CascadingParameter]
    private PopoverRootContext? RootContext { get; set; }

    [Parameter]
    public bool KeepMounted { get; set; }

    [Parameter]
    public string Container { get; set; } = "body";

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnInitialized()
    {
        portalContext = new PopoverPortalContext(KeepMounted);
    }

    protected override void OnParametersSet()
    {
        if (portalContext.KeepMounted != KeepMounted)
        {
            portalContext = new PopoverPortalContext(KeepMounted);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RootContext is null)
        {
            return;
        }

        var mounted = RootContext.GetMounted();
        var shouldRender = mounted || KeepMounted;

        if (!shouldRender)
        {
            return;
        }

        builder.OpenComponent<CascadingValue<PopoverPortalContext>>(0);
        builder.AddComponentParameter(1, "Value", portalContext);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(innerBuilder =>
        {
            innerBuilder.OpenComponent<Portal.Portal>(0);
            innerBuilder.AddAttribute(1, "Target", Container);
            innerBuilder.AddAttribute(2, "ChildContent", ChildContent);
            innerBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
