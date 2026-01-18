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
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            AddAttributes(builder, 2, resolvedClass, resolvedStyle, isButton);
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
            AddAttributes(builder, 15, resolvedClass, resolvedStyle, isButton);
            builder.AddAttribute(23, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
            builder.AddContent(24, ChildContent);
            builder.AddElementReferenceCapture(25, elementReference => Element = elementReference);
            builder.CloseElement();
        }
    }

    private void AddAttributes(RenderTreeBuilder builder, int startSequence, string? resolvedClass, string? resolvedStyle, bool isButton)
    {
        if (isButton)
        {
            builder.AddAttribute(startSequence, "type", "button");
            if (Disabled)
            {
                builder.AddAttribute(startSequence + 1, "disabled", true);
            }
        }
        else
        {
            if (Disabled)
            {
                builder.AddAttribute(startSequence + 2, "aria-disabled", "true");
            }
        }

        if (Disabled)
        {
            builder.AddAttribute(startSequence + 3, "data-disabled", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(startSequence + 4, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(startSequence + 5, "style", resolvedStyle);
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
