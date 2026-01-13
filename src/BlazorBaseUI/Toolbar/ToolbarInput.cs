using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Toolbar;

public sealed class ToolbarInput : ComponentBase, IDisposable
{
    private const string DefaultTag = "input";

    private bool hasRegistered;
    private bool isComponentRenderAs;
    private ToolbarInputState state = default!;

    [CascadingParameter]
    private ToolbarRootContext? RootContext { get; set; }

    [CascadingParameter]
    private ToolbarGroupContext? GroupContext { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool FocusableWhenDisabled { get; set; } = true;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<ToolbarInputState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<ToolbarInputState, string>? StyleValue { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnParametersSet()
    {
        if (RootContext is null)
        {
            throw new InvalidOperationException("ToolbarInput must be placed within a ToolbarRoot.");
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
            state = new ToolbarInputState(disabled, orientation, FocusableWhenDisabled);
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

        if (state.Disabled)
        {
            if (FocusableWhenDisabled)
            {
                builder.AddAttribute(3, "aria-disabled", "true");
            }
            else
            {
                builder.AddAttribute(4, "disabled", true);
            }
        }

        builder.AddAttribute(5, "data-orientation", orientationString);

        if (state.Disabled)
        {
            builder.AddAttribute(6, "data-disabled", "");
        }

        if (FocusableWhenDisabled)
        {
            builder.AddAttribute(7, "data-focusable", "");
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(8, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(9, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddComponentReferenceCapture(10, component =>
            {
                Element = ((IReferencableComponent)component).Element;
                RegisterWithToolbar();
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(11, elementReference =>
            {
                Element = elementReference;
                RegisterWithToolbar();
            });
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
