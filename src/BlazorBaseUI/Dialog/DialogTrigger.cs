using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Dialog;

public sealed class DialogTrigger : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "button";

    private bool isComponentRenderAs;
    private DialogTriggerState state;

    [CascadingParameter]
    private DialogRootContext? Context { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public Func<DialogTriggerState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<DialogTriggerState, string>? StyleValue { get; set; }

    [Parameter]
    public object? Payload { get; set; }

    [Parameter]
    public string? TriggerId { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        if (Context is null)
        {
            throw new InvalidOperationException("DialogTrigger must be used within a DialogRoot.");
        }

        var isOpenedByThis = IsOpenedByThisTrigger();
        state = new DialogTriggerState(isOpenedByThis, Disabled);
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
            var isOpenedByThis = IsOpenedByThisTrigger();
            state = new DialogTriggerState(isOpenedByThis, Disabled);
        }
    }

    private bool IsOpenedByThisTrigger()
    {
        if (Context is null || !Context.Open)
        {
            return false;
        }

        if (TriggerId is null && Context.ActiveTriggerId is null)
        {
            return true;
        }

        return TriggerId == Context.ActiveTriggerId;
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
            builder.AddAttribute(12, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
            builder.AddAttribute(13, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(14, component =>
            {
                var refComponent = (IReferencableComponent)component;
                Element = refComponent.Element;
                Context.SetTriggerElement(Element);
            });
            builder.CloseComponent();
        }
        else
        {
            builder.OpenElement(15, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(16, AdditionalAttributes);
            AddAttributes(builder, 17, resolvedClass, resolvedStyle, isButton);
            builder.AddAttribute(27, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
            builder.AddContent(28, ChildContent);
            builder.AddElementReferenceCapture(29, elementReference =>
            {
                Element = elementReference;
                Context.SetTriggerElement(Element);
            });
            builder.CloseElement();
        }
    }

    private void AddAttributes(RenderTreeBuilder builder, int startSequence, string? resolvedClass, string? resolvedStyle, bool isButton)
    {
        builder.AddAttribute(startSequence, "aria-haspopup", "dialog");
        builder.AddAttribute(startSequence + 1, "aria-expanded", Context!.Open ? "true" : "false");

        if (isButton)
        {
            builder.AddAttribute(startSequence + 2, "type", "button");
            if (Disabled)
            {
                builder.AddAttribute(startSequence + 3, "disabled", true);
            }
        }
        else
        {
            if (Disabled)
            {
                builder.AddAttribute(startSequence + 4, "aria-disabled", "true");
            }
        }

        if (Context.Open)
        {
            builder.AddAttribute(startSequence + 5, "data-popup-open", string.Empty);
        }

        if (Disabled)
        {
            builder.AddAttribute(startSequence + 6, "data-disabled", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(startSequence + 7, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(startSequence + 8, "style", resolvedStyle);
        }
    }

    private async Task HandleClick()
    {
        if (Disabled || Context is null)
        {
            return;
        }

        if (Context.Open)
        {
            await Context.SetOpenAsync(false, OpenChangeReason.TriggerPress);
        }
        else if (TriggerId is not null || Payload is not null)
        {
            await Context.SetOpenWithTriggerIdAsync(TriggerId, Payload, OpenChangeReason.TriggerPress);
        }
        else
        {
            await Context.SetOpenAsync(true, OpenChangeReason.TriggerPress);
        }
    }
}
