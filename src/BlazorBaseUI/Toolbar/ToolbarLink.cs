using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Toolbar;

public sealed class ToolbarLink : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "a";

    private bool isComponentRenderAs;
    private IReferencableComponent? componentReference;
    private ToolbarLinkState state = default!;
    private ElementReference? registeredElement;

    [CascadingParameter]
    private ToolbarRootContext? RootContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<ToolbarLinkState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<ToolbarLinkState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnParametersSet()
    {
        if (RootContext is null)
        {
            throw new InvalidOperationException("ToolbarLink must be placed within a ToolbarRoot.");
        }

        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var orientation = RootContext.Orientation;

        if (state is null || state.Orientation != orientation)
        {
            state = new ToolbarLinkState(orientation);
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
            builder.AddAttribute(2, "data-orientation", orientationString);

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(3, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(4, "style", resolvedStyle);
            }

            builder.AddComponentParameter(5, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(6, component =>
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
            builder.AddAttribute(2, "data-orientation", orientationString);

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(3, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(4, "style", resolvedStyle);
            }

            builder.AddElementReferenceCapture(5, elementReference =>
            {
                Element = elementReference;
                RegisterWithToolbar();
            });
            builder.AddContent(6, ChildContent);
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
