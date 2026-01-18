using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Dialog;

public sealed class DialogTitle : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "h2";

    private string? defaultId;
    private bool isComponentRenderAs;

    [CascadingParameter]
    private DialogRootContext? Context { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    protected override void OnInitialized()
    {
        if (Context is null)
        {
            throw new InvalidOperationException("DialogTitle must be used within a DialogRoot.");
        }

        Context.SetTitleId(ResolvedId);
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
        if (Context is null)
        {
            return;
        }

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "id", ResolvedId);
            builder.AddAttribute(3, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(4, component =>
            {
                Element = ((IReferencableComponent)component).Element;
            });
            builder.CloseComponent();
        }
        else
        {
            builder.OpenElement(5, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(6, AdditionalAttributes);
            builder.AddAttribute(7, "id", ResolvedId);
            builder.AddContent(8, ChildContent);
            builder.AddElementReferenceCapture(9, elementReference => Element = elementReference);
            builder.CloseElement();
        }
    }
}
