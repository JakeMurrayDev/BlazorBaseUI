using BlazorBaseUI.Utilities.LabelableProvider;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Globalization;

namespace BlazorBaseUI.DirectionProvider;

public sealed class DirectionProvider : ComponentBase
{
    private DirectionProviderContext context = new(Direction.Undefined);
    private Direction previousDirection;

    [Parameter]
    public Direction Direction { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnInitialized()
    {
        if (Direction == Direction.Undefined)
        {
            Direction = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? Direction.Rtl : Direction.Ltr;
        }
    }

    protected override void OnParametersSet()
    {
        if (previousDirection != Direction)
        {
            context = new DirectionProviderContext(Direction);
            previousDirection = Direction;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<DirectionProviderContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", ChildContent);
        builder.CloseComponent();
    }
}