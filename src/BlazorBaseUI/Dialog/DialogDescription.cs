using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Dialog;

public sealed class DialogDescription : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "p";

    private string? defaultId;
    private bool isComponentRenderAs;
    private DialogDescriptionState state;

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    [CascadingParameter]
    private DialogRootContext? Context { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<DialogDescriptionState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<DialogDescriptionState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        if (Context is null)
        {
            throw new InvalidOperationException("DialogDescription must be used within a DialogRoot.");
        }

        state = new DialogDescriptionState();
        Context.SetDescriptionId(ResolvedId);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (Context is not null && AttributeUtilities.GetAttributeStringValue(AdditionalAttributes, "id") != ResolvedId)
        {
            Context.SetDescriptionId(ResolvedId);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
        {
            return;
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "id", ResolvedId);

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(3, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(4, "style", resolvedStyle);
            }

            builder.AddAttribute(5, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(6, component =>
            {
                Element = ((IReferencableComponent)component).Element;
            });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "id", ResolvedId);

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(3, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(4, "style", resolvedStyle);
            }

            builder.AddContent(5, ChildContent);
            builder.AddElementReferenceCapture(6, elementReference => Element = elementReference);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }
}
