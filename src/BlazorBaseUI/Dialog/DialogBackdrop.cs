using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Dialog;

public sealed class DialogBackdrop : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";
    private const string UserSelectStyle = "user-select: none; -webkit-user-select: none;";

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
    public bool ForceRender { get; set; }

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

        if (Context.Nested && !ForceRender)
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
        var combinedStyle = CombineStyleStrings(StyleValue?.Invoke(state), UserSelectStyle);
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, combinedStyle);
        var isHidden = keepMounted && !Context.Open && (Context.TransitionStatus == TransitionStatus.Undefined || Context.TransitionStatus == TransitionStatus.Idle);

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "role", "presentation");

            if (Context.Open)
            {
                builder.AddAttribute(3, "data-open", string.Empty);
            }
            else
            {
                builder.AddAttribute(4, "data-closed", string.Empty);
            }

            if (Context.TransitionStatus == TransitionStatus.Starting)
            {
                builder.AddAttribute(5, "data-starting-style", string.Empty);
            }
            else if (Context.TransitionStatus == TransitionStatus.Ending)
            {
                builder.AddAttribute(6, "data-ending-style", string.Empty);
            }

            if (isHidden)
            {
                builder.AddAttribute(7, "hidden", string.Empty);
            }

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
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "role", "presentation");

            if (Context.Open)
            {
                builder.AddAttribute(3, "data-open", string.Empty);
            }
            else
            {
                builder.AddAttribute(4, "data-closed", string.Empty);
            }

            if (Context.TransitionStatus == TransitionStatus.Starting)
            {
                builder.AddAttribute(5, "data-starting-style", string.Empty);
            }
            else if (Context.TransitionStatus == TransitionStatus.Ending)
            {
                builder.AddAttribute(6, "data-ending-style", string.Empty);
            }

            if (isHidden)
            {
                builder.AddAttribute(7, "hidden", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(8, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(9, "style", resolvedStyle);
            }

            builder.AddAttribute(10, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
            builder.AddContent(11, ChildContent);
            builder.AddElementReferenceCapture(12, elementReference => Element = elementReference);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    private async Task HandleClick(MouseEventArgs e)
    {
        if (Context is not null && Context.DismissOnOutsidePress)
        {
            await Context.SetOpenAsync(false, OpenChangeReason.OutsidePress);
        }

        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
    }

    private static string? CombineStyleStrings(string? style1, string? style2)
    {
        if (string.IsNullOrEmpty(style1) && string.IsNullOrEmpty(style2))
        {
            return null;
        }

        if (string.IsNullOrEmpty(style1))
        {
            return style2;
        }

        if (string.IsNullOrEmpty(style2))
        {
            return style1;
        }

        var separator = style1.TrimEnd().EndsWith(';') ? " " : "; ";
        return $"{style1}{separator}{style2}";
    }
}
