using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Dialog;

/// <summary>
/// A positioning container for the dialog popup that can be made scrollable.
/// Renders a div element.
/// </summary>
public sealed class DialogViewport : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;
    private DialogViewportState state;

    [CascadingParameter]
    private DialogRootContext? Context { get; set; }

    [CascadingParameter]
    private DialogPortalContext? PortalContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<DialogViewportState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<DialogViewportState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        if (Context is null)
        {
            throw new InvalidOperationException("DialogViewport must be used within a DialogRoot.");
        }

        state = new DialogViewportState(Context.Open, Context.TransitionStatus, Context.Nested, Context.NestedDialogCount > 0);
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
            state = new DialogViewportState(Context.Open, Context.TransitionStatus, Context.Nested, Context.NestedDialogCount > 0);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
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

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "role", "presentation");

            if (!Context.Mounted)
            {
                builder.AddAttribute(3, "hidden", string.Empty);
            }

            if (Context.Open)
            {
                builder.AddAttribute(4, "data-open", string.Empty);
            }
            else
            {
                builder.AddAttribute(5, "data-closed", string.Empty);
            }

            if (Context.TransitionStatus == TransitionStatus.Starting)
            {
                builder.AddAttribute(6, "data-starting-style", string.Empty);
            }
            else if (Context.TransitionStatus == TransitionStatus.Ending)
            {
                builder.AddAttribute(7, "data-ending-style", string.Empty);
            }

            if (Context.Nested)
            {
                builder.AddAttribute(8, "data-nested", string.Empty);
            }

            if (Context.NestedDialogCount > 0)
            {
                builder.AddAttribute(9, "data-nested-dialog-open", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(10, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(11, "style", resolvedStyle);
            }

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
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "role", "presentation");

            if (!Context.Mounted)
            {
                builder.AddAttribute(3, "hidden", string.Empty);
            }

            if (Context.Open)
            {
                builder.AddAttribute(4, "data-open", string.Empty);
            }
            else
            {
                builder.AddAttribute(5, "data-closed", string.Empty);
            }

            if (Context.TransitionStatus == TransitionStatus.Starting)
            {
                builder.AddAttribute(6, "data-starting-style", string.Empty);
            }
            else if (Context.TransitionStatus == TransitionStatus.Ending)
            {
                builder.AddAttribute(7, "data-ending-style", string.Empty);
            }

            if (Context.Nested)
            {
                builder.AddAttribute(8, "data-nested", string.Empty);
            }

            if (Context.NestedDialogCount > 0)
            {
                builder.AddAttribute(9, "data-nested-dialog-open", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(10, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(11, "style", resolvedStyle);
            }

            builder.AddContent(12, ChildContent);
            builder.AddElementReferenceCapture(13, elementReference => Element = elementReference);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }
}
