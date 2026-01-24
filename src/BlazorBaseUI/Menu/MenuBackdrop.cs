using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Menu;

public sealed class MenuBackdrop : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;
    private MenuBackdropState state;

    [CascadingParameter]
    private MenuRootContext? RootContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<MenuBackdropState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<MenuBackdropState, string>? StyleValue { get; set; }

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

        var open = RootContext?.Open ?? false;
        var transitionStatus = RootContext?.TransitionStatus ?? TransitionStatus.None;
        state = new MenuBackdropState(open, transitionStatus);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RootContext is null)
        {
            return;
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            RenderAttributes(builder, resolvedClass, resolvedStyle);
            builder.AddAttribute(12, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(13, component =>
            {
                Element = ((IReferencableComponent)component).Element;
            });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            RenderAttributes(builder, resolvedClass, resolvedStyle);
            builder.AddContent(12, ChildContent);
            builder.AddElementReferenceCapture(13, elementReference => Element = elementReference);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    private void RenderAttributes(RenderTreeBuilder builder, string? resolvedClass, string? resolvedStyle)
    {
        var open = RootContext!.Open;
        var mounted = RootContext.Mounted;
        var transitionStatus = RootContext.TransitionStatus;
        var openChangeReason = RootContext.OpenChangeReason;

        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "role", "presentation");

        if (!mounted)
        {
            builder.AddAttribute(3, "hidden", true);
        }

        if (open)
        {
            builder.AddAttribute(4, "data-open", string.Empty);
        }
        else
        {
            builder.AddAttribute(5, "data-closed", string.Empty);
        }

        if (transitionStatus == TransitionStatus.Starting)
        {
            builder.AddAttribute(6, "data-starting-style", string.Empty);
        }
        else if (transitionStatus == TransitionStatus.Ending)
        {
            builder.AddAttribute(7, "data-ending-style", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(8, "class", resolvedClass);
        }

        var pointerEvents = openChangeReason == OpenChangeReason.TriggerHover ? "none" : null;
        var combinedStyle = "user-select: none; -webkit-user-select: none;";
        if (!string.IsNullOrEmpty(pointerEvents))
        {
            combinedStyle = $"pointer-events: {pointerEvents}; {combinedStyle}";
        }
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            combinedStyle = $"{combinedStyle} {resolvedStyle}";
        }
        builder.AddAttribute(9, "style", combinedStyle);

        builder.AddAttribute(10, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));
    }

    private async Task HandleClickAsync(MouseEventArgs e)
    {
        if (RootContext is null)
        {
            return;
        }

        await RootContext.SetOpenAsync(false, OpenChangeReason.OutsidePress, null);
        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
    }
}
