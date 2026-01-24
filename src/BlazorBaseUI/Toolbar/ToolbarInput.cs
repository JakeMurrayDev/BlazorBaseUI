using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Toolbar;

public sealed class ToolbarInput : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "input";

    private bool isComponentRenderAs;
    private IReferencableComponent? componentReference;
    private ToolbarInputState state = default!;
    private ElementReference? registeredElement;

    [CascadingParameter]
    private ToolbarRootContext? RootContext { get; set; }

    [CascadingParameter]
    private ToolbarGroupContext? GroupContext { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool FocusableWhenDisabled { get; set; } = true;

    [Parameter]
    public string? DefaultValue { get; set; }

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
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);

            if (state.Disabled)
            {
                if (FocusableWhenDisabled)
                {
                    builder.AddAttribute(2, "aria-disabled", "true");
                }
                else
                {
                    builder.AddAttribute(3, "disabled", true);
                }
            }

            if (DefaultValue is not null)
            {
                builder.AddAttribute(4, "value", DefaultValue);
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

            builder.AddComponentReferenceCapture(10, component =>
            {
                componentReference = (IReferencableComponent)component;
            });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);

            if (state.Disabled)
            {
                if (FocusableWhenDisabled)
                {
                    builder.AddAttribute(2, "aria-disabled", "true");
                }
                else
                {
                    builder.AddAttribute(3, "disabled", true);
                }
            }

            if (DefaultValue is not null)
            {
                builder.AddAttribute(4, "value", DefaultValue);
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

            builder.AddElementReferenceCapture(10, elementReference =>
            {
                Element = elementReference;
                RegisterWithToolbar();
            });
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (isComponentRenderAs && componentReference is not null)
        {
            var newElement = componentReference.Element;
            if (newElement.HasValue && !Equals(newElement, Element))
            {
                Element = newElement;
                RegisterWithToolbar();
            }
        }
    }

    private void RegisterWithToolbar()
    {
        if (!Element.HasValue || RootContext is null)
        {
            return;
        }

        if (registeredElement.HasValue && !registeredElement.Value.Equals(Element.Value))
        {
            RootContext.UnregisterItem(registeredElement.Value);
        }

        if (!registeredElement.HasValue || !registeredElement.Value.Equals(Element.Value))
        {
            RootContext.RegisterItem(Element.Value);
            registeredElement = Element;
        }
    }

    public void Dispose()
    {
        if (registeredElement.HasValue && RootContext is not null)
        {
            RootContext.UnregisterItem(registeredElement.Value);
        }
    }
}
