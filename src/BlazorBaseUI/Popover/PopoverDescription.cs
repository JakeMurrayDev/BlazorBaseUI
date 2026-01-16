using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Popover;

public sealed class PopoverDescription : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "p";

    private string descriptionId = string.Empty;
    private bool isComponentRenderAs;
    private IReferencableComponent? componentReference;

    [CascadingParameter]
    private PopoverRootContext? RootContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        descriptionId = AttributeUtilities.GetAttributeStringValue(AdditionalAttributes, "id")
            ?? $"popover-description-{Guid.NewGuid():N}";

        RootContext?.SetDescriptionId(descriptionId);
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
        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "id", descriptionId);

        if (isComponentRenderAs)
        {
            builder.AddAttribute(3, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(4, component =>
            {
                componentReference = (IReferencableComponent)component;
                Element = componentReference.Element;
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddContent(5, ChildContent);
            builder.AddElementReferenceCapture(6, elementReference => Element = elementReference);
            builder.CloseElement();
        }
    }

    public void Dispose()
    {
        RootContext?.SetDescriptionId(string.Empty);
    }
}
