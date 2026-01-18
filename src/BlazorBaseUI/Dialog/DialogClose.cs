using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Dialog;

public sealed class DialogClose : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "button";

    private bool isComponentRenderAs;
    private DialogCloseState state;

    [CascadingParameter]
    private DialogRootContext? Context { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public Func<DialogCloseState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<DialogCloseState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        if (Context is null)
        {
            throw new InvalidOperationException("DialogClose must be used within a DialogRoot.");
        }

        state = new DialogCloseState(Disabled);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        state = new DialogCloseState(Disabled);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
        {
            return;
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var isButton = string.IsNullOrEmpty(As) || As == "button";

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);

            if (isButton)
            {
                builder.AddAttribute(2, "type", "button");
                if (Disabled)
                {
                    builder.AddAttribute(3, "disabled", true);
                }
            }
            else
            {
                if (Disabled)
                {
                    builder.AddAttribute(4, "aria-disabled", "true");
                }
            }

            if (Disabled)
            {
                builder.AddAttribute(5, "data-disabled", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(6, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(7, "style", resolvedStyle);
            }

            builder.AddAttribute(8, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
            builder.AddAttribute(9, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(10, component =>
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

            if (isButton)
            {
                builder.AddAttribute(2, "type", "button");
                if (Disabled)
                {
                    builder.AddAttribute(3, "disabled", true);
                }
            }
            else
            {
                if (Disabled)
                {
                    builder.AddAttribute(4, "aria-disabled", "true");
                }
            }

            if (Disabled)
            {
                builder.AddAttribute(5, "data-disabled", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(6, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(7, "style", resolvedStyle);
            }

            builder.AddAttribute(8, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
            builder.AddContent(9, ChildContent);
            builder.AddElementReferenceCapture(10, elementReference => Element = elementReference);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    private async Task HandleClick()
    {
        if (Disabled || Context is null)
        {
            return;
        }

        await Context.SetOpenAsync(false, OpenChangeReason.ClosePress);
    }
}
