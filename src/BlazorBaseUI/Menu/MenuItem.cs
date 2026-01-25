using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Menu;

public sealed class MenuItem : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;
    private bool highlighted;
    private bool hasMouseMoveAttribute;
    private MenuItemState state;

    [CascadingParameter]
    private MenuRootContext? RootContext { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool CloseOnClick { get; set; } = true;

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<MenuItemState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<MenuItemState, string>? StyleValue { get; set; }

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

        hasMouseMoveAttribute = AttributeUtilities.HasAttribute(AdditionalAttributes, "onmousemove");
        state = new MenuItemState(Disabled, highlighted);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "role", "menuitem");

            if (!string.IsNullOrEmpty(Id))
            {
                builder.AddAttribute(3, "id", Id);
            }

            builder.AddAttribute(4, "tabindex", highlighted ? 0 : -1);

            if (Disabled)
            {
                builder.AddAttribute(5, "aria-disabled", "true");
                builder.AddAttribute(6, "data-disabled", string.Empty);
            }

            if (highlighted)
            {
                builder.AddAttribute(7, "data-highlighted", string.Empty);
            }

            if (!string.IsNullOrEmpty(Label))
            {
                builder.AddAttribute(8, "data-label", Label);
            }

            builder.AddAttribute(9, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));
            builder.AddAttribute(10, "onmouseenter", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseEnterAsync));
            builder.AddAttribute(11, "onmouseleave", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseLeaveAsync));
            builder.AddAttribute(12, "onmousemove", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseMoveAsync));

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(13, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(14, "style", resolvedStyle);
            }

            builder.AddComponentParameter(15, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(16, component => Element = ((IReferencableComponent)component).Element);
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "role", "menuitem");

            if (!string.IsNullOrEmpty(Id))
            {
                builder.AddAttribute(3, "id", Id);
            }

            builder.AddAttribute(4, "tabindex", highlighted ? 0 : -1);

            if (Disabled)
            {
                builder.AddAttribute(5, "aria-disabled", "true");
                builder.AddAttribute(6, "data-disabled", string.Empty);
            }

            if (highlighted)
            {
                builder.AddAttribute(7, "data-highlighted", string.Empty);
            }

            if (!string.IsNullOrEmpty(Label))
            {
                builder.AddAttribute(8, "data-label", Label);
            }

            builder.AddAttribute(9, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));
            builder.AddAttribute(10, "onmouseenter", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseEnterAsync));
            builder.AddAttribute(11, "onmouseleave", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseLeaveAsync));
            builder.AddAttribute(12, "onmousemove", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseMoveAsync));

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(13, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(14, "style", resolvedStyle);
            }

            builder.AddElementReferenceCapture(15, elementReference => Element = elementReference);
            builder.AddContent(16, ChildContent);
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

        if (CloseOnClick && RootContext is not null)
        {
            RootContext.EmitClose(OpenChangeReason.ItemPress, null);
        }

        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
    }

    private async Task HandleMouseEnterAsync(MouseEventArgs e)
    {
        var shouldHighlight = RootContext?.HighlightItemOnHover ?? true;
        if (!Disabled && shouldHighlight)
        {
            highlighted = true;
            state = state with { Highlighted = true };
            StateHasChanged();
        }

        await EventUtilities.InvokeOnMouseEnterAsync(AdditionalAttributes, e);
    }

    private async Task HandleMouseLeaveAsync(MouseEventArgs e)
    {
        var shouldHighlight = RootContext?.HighlightItemOnHover ?? true;
        if (shouldHighlight)
        {
            highlighted = false;
            state = state with { Highlighted = false };
            StateHasChanged();
        }

        await EventUtilities.InvokeOnMouseLeaveAsync(AdditionalAttributes, e);
    }

    private Task HandleMouseMoveAsync(MouseEventArgs e)
    {
        var shouldHighlight = RootContext?.HighlightItemOnHover ?? true;
        if (!Disabled && !highlighted && shouldHighlight)
        {
            highlighted = true;
            state = state with { Highlighted = true };
            StateHasChanged();
        }

        return hasMouseMoveAttribute
            ? EventUtilities.InvokeOnMouseMoveAsync(AdditionalAttributes, e)
            : Task.CompletedTask;
    }
}
