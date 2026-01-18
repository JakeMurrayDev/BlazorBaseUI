using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tooltip;

public sealed class TooltipProvider : ComponentBase
{
    private DateTime? lastClosedTime;
    private TooltipProviderContext context = null!;

    [Parameter]
    public int? Delay { get; set; }

    [Parameter]
    public int? CloseDelay { get; set; }

    [Parameter]
    public int Timeout { get; set; } = 400;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnInitialized()
    {
        context = CreateContext();
    }

    protected override void OnParametersSet()
    {
        context = CreateContext();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<TooltipProviderContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    private TooltipProviderContext CreateContext() => new(
        Delay: Delay,
        CloseDelay: CloseDelay,
        Timeout: Timeout,
        GetLastClosedTime: () => lastClosedTime,
        SetLastClosedTime: SetLastClosedTime);

    private void SetLastClosedTime()
    {
        lastClosedTime = DateTime.UtcNow;
    }
}
