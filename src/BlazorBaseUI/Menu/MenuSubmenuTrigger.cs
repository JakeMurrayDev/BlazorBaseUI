using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Menu;

public sealed class MenuSubmenuTrigger : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;
    private bool highlighted;
    private bool open;

    [CascadingParameter]
    private MenuSubmenuRootContext? SubmenuContext { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool OpenOnHover { get; set; } = true;

    [Parameter]
    public int Delay { get; set; } = 100;

    [Parameter]
    public int CloseDelay { get; set; }

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<MenuSubmenuTriggerState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<MenuSubmenuTriggerState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        if (SubmenuContext is null)
        {
            throw new InvalidOperationException("MenuSubmenuTrigger must be placed inside a MenuSubmenuRoot.");
        }
    }

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
        var state = new MenuSubmenuTriggerState(
            Disabled: Disabled,
            Highlighted: highlighted,
            Open: open);

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "role", "menuitem");
            builder.AddAttribute(3, "aria-haspopup", "menu");
            builder.AddAttribute(4, "aria-expanded", open ? "true" : "false");
            builder.AddAttribute(5, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));

            if (Disabled)
            {
                builder.AddAttribute(6, "aria-disabled", "true");
            }

            builder.AddAttribute(7, "tabindex", open || highlighted ? 0 : -1);
            builder.AddAttribute(8, "onmouseenter", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseEnter));
            builder.AddAttribute(9, "onmouseleave", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseLeave));
            builder.AddAttribute(10, "onblur", EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlur));

            if (open)
            {
                builder.AddAttribute(11, "data-open", string.Empty);
            }
            else
            {
                builder.AddAttribute(12, "data-closed", string.Empty);
            }

            if (Disabled)
            {
                builder.AddAttribute(13, "data-disabled", string.Empty);
            }

            if (highlighted)
            {
                builder.AddAttribute(14, "data-highlighted", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(15, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(16, "style", resolvedStyle);
            }

            builder.AddComponentParameter(17, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(18, component => Element = ((IReferencableComponent)component).Element);
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "role", "menuitem");
            builder.AddAttribute(3, "aria-haspopup", "menu");
            builder.AddAttribute(4, "aria-expanded", open ? "true" : "false");
            builder.AddAttribute(5, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));

            if (Disabled)
            {
                builder.AddAttribute(6, "aria-disabled", "true");
            }

            builder.AddAttribute(7, "tabindex", open || highlighted ? 0 : -1);
            builder.AddAttribute(8, "onmouseenter", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseEnter));
            builder.AddAttribute(9, "onmouseleave", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseLeave));
            builder.AddAttribute(10, "onblur", EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlur));

            if (open)
            {
                builder.AddAttribute(11, "data-open", string.Empty);
            }
            else
            {
                builder.AddAttribute(12, "data-closed", string.Empty);
            }

            if (Disabled)
            {
                builder.AddAttribute(13, "data-disabled", string.Empty);
            }

            if (highlighted)
            {
                builder.AddAttribute(14, "data-highlighted", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(15, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(16, "style", resolvedStyle);
            }

            builder.AddElementReferenceCapture(17, elementReference => Element = elementReference);
            builder.AddContent(18, ChildContent);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    private async Task HandleClickAsync(MouseEventArgs e)
    {
        if (Disabled)
        {
            return;
        }

        open = !open;
        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
    }

    private void HandleMouseEnter(MouseEventArgs e)
    {
        if (!Disabled)
        {
            highlighted = true;
        }
    }

    private void HandleMouseLeave(MouseEventArgs e)
    {
        highlighted = false;
    }

    private void HandleBlur(FocusEventArgs e)
    {
        if (highlighted)
        {
            highlighted = false;
        }
    }
}
