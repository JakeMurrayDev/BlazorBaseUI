using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Dialog;

public sealed class DialogBackdrop : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;
    private DialogBackdropState state;

    [CascadingParameter]
    private DialogRootContext? Context { get; set; }

    [CascadingParameter]
    private DialogPortalContext? PortalContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<DialogBackdropState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<DialogBackdropState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        if (Context is null)
        {
            throw new InvalidOperationException("DialogBackdrop must be used within a DialogRoot.");
        }

        state = new DialogBackdropState(Context.Open, Context.TransitionStatus);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (Context is not null)
        {
            state = new DialogBackdropState(Context.Open, Context.TransitionStatus);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
        {
            return;
        }

        if (Context.Modal == ModalMode.False)
        {
            return;
        }

        var keepMounted = PortalContext?.KeepMounted ?? false;
        var shouldRender = keepMounted || Context.Mounted;

        if (!shouldRender)
        {
            return;
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var isHidden = keepMounted && !Context.Open && (Context.TransitionStatus == TransitionStatus.Undefined || Context.TransitionStatus == TransitionStatus.Idle);

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "role", "presentation");
            AddDataAttributes(builder, 3, isHidden);

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(8, "class", resolvedClass);
            }
            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(9, "style", resolvedStyle);
            }

            builder.AddAttribute(10, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
            builder.AddAttribute(11, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(12, component =>
            {
                Element = ((IReferencableComponent)component).Element;
            });
            builder.CloseComponent();
        }
        else
        {
            builder.OpenElement(13, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(14, AdditionalAttributes);
            builder.AddAttribute(15, "role", "presentation");
            AddDataAttributes(builder, 16, isHidden);

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(21, "class", resolvedClass);
            }
            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(22, "style", resolvedStyle);
            }

            builder.AddAttribute(23, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
            builder.AddContent(24, ChildContent);
            builder.AddElementReferenceCapture(25, elementReference => Element = elementReference);
            builder.CloseElement();
        }
    }

    private void AddDataAttributes(RenderTreeBuilder builder, int startSequence, bool isHidden)
    {
        if (Context!.Open)
        {
            builder.AddAttribute(startSequence, "data-open", string.Empty);
        }
        else
        {
            builder.AddAttribute(startSequence + 1, "data-closed", string.Empty);
        }

        if (Context.TransitionStatus == TransitionStatus.Starting)
        {
            builder.AddAttribute(startSequence + 2, "data-starting-style", string.Empty);
        }
        else if (Context.TransitionStatus == TransitionStatus.Ending)
        {
            builder.AddAttribute(startSequence + 3, "data-ending-style", string.Empty);
        }

        if (isHidden)
        {
            builder.AddAttribute(startSequence + 4, "hidden", string.Empty);
        }
    }

    private async Task HandleClick()
    {
        if (Context is not null && Context.DismissOnOutsidePress)
        {
            await Context.SetOpenAsync(false, OpenChangeReason.OutsidePress);
        }
    }
}
