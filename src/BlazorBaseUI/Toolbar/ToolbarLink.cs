using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Toolbar;

public sealed class ToolbarLink : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "a";

    private bool hasRegistered;
    private bool isComponentRenderAs;
    private IReferencableComponent? componentReference;
    private ToolbarLinkState state = default!;

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
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(1, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(2, AdditionalAttributes);
        builder.AddAttribute(3, "data-orientation", orientationString);
        builder.AddAttribute(4, "data-focusable", "");

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(5, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(6, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(7, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(8, component =>
            {
                componentReference = (IReferencableComponent)component;
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(9, elementReference =>
            {
                Element = elementReference;
                RegisterWithToolbar();
            });
            builder.AddContent(10, ChildContent);
            builder.CloseElement();
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
