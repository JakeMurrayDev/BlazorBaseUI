using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Globalization;

namespace BlazorBaseUI.DirectionProvider;

/// <summary>
/// Enables RTL behavior for Base UI components.
/// </summary>
public sealed class DirectionProvider : ComponentBase
{
    private DirectionProviderContext context = new(Direction.Undefined);
    private Direction previousDirection;

    /// <summary>
    /// Gets or sets the reading direction of the text. Defaults to <see cref="BlazorBaseUI.Direction.Undefined"/>,
    /// which resolves based on <see cref="CultureInfo.CurrentCulture"/>.
    /// </summary>
    [Parameter]
    public Direction Direction { get; set; }

    /// <summary>
    /// Defines the child components of this instance.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Direction == Direction.Undefined)
        {
            Direction = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? Direction.Rtl : Direction.Ltr;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (previousDirection != Direction)
        {
            context = new DirectionProviderContext(Direction);
            previousDirection = Direction;
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<DirectionProviderContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", ChildContent);
        builder.CloseComponent();
    }
}