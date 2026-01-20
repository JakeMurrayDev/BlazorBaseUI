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
    public bool NativeButton { get; set; } = true;

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
        var isNativeButton = NativeButton && (string.IsNullOrEmpty(As) || As == "button");

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);

            if (isNativeButton)
            {
                builder.AddAttribute(2, "type", "button");
                if (Disabled)
                {
                    builder.AddAttribute(3, "disabled", true);
                }
            }
            else
            {
                builder.AddAttribute(4, "role", "button");
                builder.AddAttribute(5, "tabindex", Disabled ? "-1" : "0");
                if (Disabled)
                {
                    builder.AddAttribute(6, "aria-disabled", "true");
                }
            }

            if (Disabled)
            {
                builder.AddAttribute(7, "data-disabled", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(8, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(9, "style", resolvedStyle);
            }

            builder.AddAttribute(10, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));

            if (!isNativeButton)
            {
                builder.AddAttribute(11, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync));
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

            if (isNativeButton)
            {
                builder.AddAttribute(2, "type", "button");
                if (Disabled)
                {
                    builder.AddAttribute(3, "disabled", true);
                }
            }
            else
            {
                builder.AddAttribute(4, "role", "button");
                builder.AddAttribute(5, "tabindex", Disabled ? "-1" : "0");
                if (Disabled)
                {
                    builder.AddAttribute(6, "aria-disabled", "true");
                }
            }

            if (Disabled)
            {
                builder.AddAttribute(7, "data-disabled", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(8, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(9, "style", resolvedStyle);
            }

            builder.AddAttribute(10, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));

            if (!isNativeButton)
            {
                builder.AddAttribute(11, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync));
            }

            builder.AddContent(12, ChildContent);
            builder.AddElementReferenceCapture(13, elementReference => Element = elementReference);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    private async Task HandleClickAsync(MouseEventArgs e)
    {
        if (Disabled || Context is null || !Context.GetOpen())
        {
            return;
        }

        await Context.SetOpenAsync(false, OpenChangeReason.ClosePress);
        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (Disabled || Context is null || !Context.GetOpen())
        {
            return;
        }

        if (e.Key == "Enter" || e.Key == " ")
        {
            await Context.SetOpenAsync(false, OpenChangeReason.ClosePress);
        }
    }
}
