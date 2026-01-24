using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Menu;

public sealed class MenuCheckboxItem : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;
    private bool highlighted;
    private bool internalChecked;
    private MenuCheckboxItemContext? itemContext;

    private bool IsControlled => Checked.HasValue;

    private bool IsChecked => IsControlled ? Checked!.Value : internalChecked;

    [CascadingParameter]
    private MenuRootContext? RootContext { get; set; }

    [Parameter]
    public bool? Checked { get; set; }

    [Parameter]
    public bool DefaultChecked { get; set; }

    [Parameter]
    public bool CloseOnClick { get; set; }

    [Parameter]
    public EventCallback<MenuCheckboxItemChangeEventArgs> OnCheckedChange { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<MenuCheckboxItemState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<MenuCheckboxItemState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        if (!IsControlled)
        {
            internalChecked = DefaultChecked;
        }
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        itemContext = new MenuCheckboxItemContext(
            Checked: IsChecked,
            Highlighted: highlighted,
            Disabled: Disabled);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<MenuCheckboxItemContext>>(0);
        builder.AddComponentParameter(1, "Value", itemContext);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)RenderContent);
        builder.CloseComponent();
    }

    private void RenderContent(RenderTreeBuilder builder)
    {
        var state = new MenuCheckboxItemState(
            Disabled: Disabled,
            Highlighted: highlighted,
            Checked: IsChecked);

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "role", "menuitemcheckbox");
            builder.AddAttribute(3, "aria-checked", IsChecked ? "true" : "false");
            builder.AddAttribute(4, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));

            if (Disabled)
            {
                builder.AddAttribute(5, "aria-disabled", "true");
            }

            builder.AddAttribute(6, "tabindex", highlighted ? 0 : -1);
            builder.AddAttribute(7, "onmouseenter", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseEnterAsync));
            builder.AddAttribute(8, "onmouseleave", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseLeaveAsync));

            if (IsChecked)
            {
                builder.AddAttribute(9, "data-checked", string.Empty);
            }
            else
            {
                builder.AddAttribute(10, "data-unchecked", string.Empty);
            }

            if (Disabled)
            {
                builder.AddAttribute(11, "data-disabled", string.Empty);
            }

            if (highlighted)
            {
                builder.AddAttribute(12, "data-highlighted", string.Empty);
            }

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
            builder.AddAttribute(2, "role", "menuitemcheckbox");
            builder.AddAttribute(3, "aria-checked", IsChecked ? "true" : "false");
            builder.AddAttribute(4, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));

            if (Disabled)
            {
                builder.AddAttribute(5, "aria-disabled", "true");
            }

            builder.AddAttribute(6, "tabindex", highlighted ? 0 : -1);
            builder.AddAttribute(7, "onmouseenter", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseEnterAsync));
            builder.AddAttribute(8, "onmouseleave", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseLeaveAsync));

            if (IsChecked)
            {
                builder.AddAttribute(9, "data-checked", string.Empty);
            }
            else
            {
                builder.AddAttribute(10, "data-unchecked", string.Empty);
            }

            if (Disabled)
            {
                builder.AddAttribute(11, "data-disabled", string.Empty);
            }

            if (highlighted)
            {
                builder.AddAttribute(12, "data-highlighted", string.Empty);
            }

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

        var newChecked = !IsChecked;
        var eventArgs = new MenuCheckboxItemChangeEventArgs(newChecked);
        await OnCheckedChange.InvokeAsync(eventArgs);

        if (eventArgs.IsCanceled)
        {
            return;
        }

        if (!IsControlled)
        {
            internalChecked = newChecked;
        }

        if (CloseOnClick && RootContext is not null)
        {
            RootContext.EmitClose(OpenChangeReason.ItemPress, null);
        }

        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
    }

    private async Task HandleMouseEnterAsync(MouseEventArgs e)
    {
        if (!Disabled && itemContext is not null)
        {
            highlighted = true;
            itemContext = itemContext with { Highlighted = true };
        }

        await EventUtilities.InvokeOnMouseEnterAsync(AdditionalAttributes, e);
    }

    private async Task HandleMouseLeaveAsync(MouseEventArgs e)
    {
        highlighted = false;
        if (itemContext is not null)
        {
            itemContext = itemContext with { Highlighted = false };
        }

        await EventUtilities.InvokeOnMouseLeaveAsync(AdditionalAttributes, e);
    }
}
