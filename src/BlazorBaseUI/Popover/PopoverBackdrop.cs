using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Popover;

public sealed class PopoverBackdrop : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;
    private IReferencableComponent? componentReference;
    private PopoverBackdropState state;

    [CascadingParameter]
    private PopoverRootContext? RootContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<PopoverBackdropState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<PopoverBackdropState, string>? StyleValue { get; set; }

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

        var open = RootContext?.GetOpen() ?? false;
        var transitionStatus = RootContext?.TransitionStatus ?? TransitionStatus.None;
        state = new PopoverBackdropState(open, transitionStatus);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RootContext is null)
        {
            return;
        }

        var open = RootContext.GetOpen();
        var mounted = RootContext.GetMounted();
        var transitionStatus = RootContext.TransitionStatus;
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

        var combinedStyle = "user-select: none; -webkit-user-select: none;";
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            combinedStyle = $"{combinedStyle} {resolvedStyle}";
        }
        builder.AddAttribute(9, "style", combinedStyle);

        builder.AddAttribute(10, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));

        if (isComponentRenderAs)
        {
            builder.AddAttribute(11, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(12, component =>
            {
                componentReference = (IReferencableComponent)component;
                Element = componentReference.Element;
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddContent(13, ChildContent);
            builder.AddElementReferenceCapture(14, elementReference => Element = elementReference);
            builder.CloseElement();
        }
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
