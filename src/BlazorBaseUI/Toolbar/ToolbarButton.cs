using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Toolbar;

public sealed class ToolbarButton : ComponentBase, IDisposable
{
    private const string DefaultTag = "button";

    private bool hasRegistered;
    private bool isComponentRenderAs;
    private ToolbarButtonState state = default!;

    [CascadingParameter]
    private ToolbarRootContext? RootContext { get; set; }

    [CascadingParameter]
    private ToolbarGroupContext? GroupContext { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool FocusableWhenDisabled { get; set; } = true;

    [Parameter]
    public bool NativeButton { get; set; } = true;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<ToolbarButtonState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<ToolbarButtonState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnParametersSet()
    {
        if (RootContext is null)
        {
            throw new InvalidOperationException("ToolbarButton must be placed within a ToolbarRoot.");
        }

        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var disabled = RootContext.Disabled || (GroupContext?.Disabled ?? false) || Disabled;
        var orientation = RootContext.Orientation;

        if (state is null || state.Disabled != disabled || state.Orientation != orientation || state.Focusable != FocusableWhenDisabled)
        {
            state = new ToolbarButtonState(disabled, orientation, FocusableWhenDisabled);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var orientationString = state.Orientation.ToDataAttributeString();

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(1, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(2, AdditionalAttributes);

        if (NativeButton)
        {
            builder.AddAttribute(3, "type", "button");
            if (state.Disabled && FocusableWhenDisabled)
            {
                builder.AddAttribute(4, "aria-disabled", "true");
            }
            else if (state.Disabled)
            {
                builder.AddAttribute(5, "disabled", true);
            }
        }
        else
        {
            builder.AddAttribute(6, "role", "button");
            if (state.Disabled)
            {
                builder.AddAttribute(7, "aria-disabled", "true");
            }
        }

        builder.AddAttribute(8, "data-orientation", orientationString);

        if (state.Disabled)
        {
            builder.AddAttribute(9, "data-disabled", "");
        }

        if (FocusableWhenDisabled)
        {
            builder.AddAttribute(10, "data-focusable", "");
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(11, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(12, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(13, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(14, component =>
            {
                Element = ((IReferencableComponent)component).Element;
                RegisterWithToolbar();
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(15, elementReference =>
            {
                Element = elementReference;
                RegisterWithToolbar();
            });
            builder.AddContent(16, ChildContent);
            builder.CloseElement();
        }
    }

    private void RegisterWithToolbar()
    {
        if (hasRegistered || !Element.HasValue || RootContext is null)
        {
            return;
        }

        hasRegistered = true;
        RootContext.RegisterItem(Element.Value);
    }

    public void Dispose()
    {
        if (hasRegistered && Element.HasValue && RootContext is not null)
        {
            RootContext.UnregisterItem(Element.Value);
        }
    }
}
