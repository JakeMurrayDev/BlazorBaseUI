using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tooltip;

public sealed class TooltipPortal : ComponentBase
{
    private TooltipPortalContext portalContext = null!;

    [CascadingParameter]
    private TooltipRootContext? RootContext { get; set; }

    [Parameter]
    public bool KeepMounted { get; set; }

    [Parameter]
    public string Container { get; set; } = "body";

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnInitialized()
    {
        portalContext = new TooltipPortalContext(KeepMounted);
    }

    protected override void OnParametersSet()
    {
        if (portalContext.KeepMounted != KeepMounted)
        {
            portalContext = new TooltipPortalContext(KeepMounted);
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

        builder.OpenComponent<CascadingValue<TooltipPortalContext>>(0);
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
