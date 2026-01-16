using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Popover;

public sealed class PopoverClose : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "button";

    private bool isComponentRenderAs;
    private IReferencableComponent? componentReference;

    [CascadingParameter]
    private PopoverRootContext? RootContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool NativeButton { get; set; } = true;

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
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);

        if (!NativeButton && string.IsNullOrEmpty(As))
        {
            builder.AddAttribute(2, "role", "button");
            if (Disabled)
            {
                builder.AddAttribute(3, "aria-disabled", "true");
            }
        }

        if (NativeButton || string.IsNullOrEmpty(As) || As == "button")
        {
            builder.AddAttribute(4, "type", "button");
            if (Disabled)
            {
                builder.AddAttribute(5, "disabled", true);
            }
        }

        builder.AddAttribute(6, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));

        if (isComponentRenderAs)
        {
            builder.AddAttribute(7, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(8, component =>
            {
                componentReference = (IReferencableComponent)component;
                Element = componentReference.Element;
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddContent(9, ChildContent);
            builder.AddElementReferenceCapture(10, elementReference => Element = elementReference);
            builder.CloseElement();
        }
    }

    private async Task HandleClickAsync(MouseEventArgs e)
    {
        if (Disabled || RootContext is null)
        {
            return;
        }

        await RootContext.SetOpenAsync(false, OpenChangeReason.ClosePress);
        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
    }
}
