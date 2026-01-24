using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Menu;

public sealed class MenuGroupLabel : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "div";

    private static readonly MenuGroupLabelState CachedState = new();

    private bool isComponentRenderAs;
    private string id = default!;
    private bool hasRegisteredId;

    [CascadingParameter]
    private IMenuGroupContext? GroupContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<MenuGroupLabelState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<MenuGroupLabelState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        id = AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => $"menu-group-label-{Guid.NewGuid():N}");
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var newId = AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => id);
        if (newId != id)
        {
            id = newId;
            GroupContext?.SetLabelId(id);
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && !hasRegisteredId)
        {
            GroupContext?.SetLabelId(id);
            hasRegisteredId = true;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(CachedState));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(CachedState));

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "id", id);
            builder.AddAttribute(3, "role", "presentation");

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(4, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(5, "style", resolvedStyle);
            }

            builder.AddComponentParameter(6, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(7, component => Element = ((IReferencableComponent)component).Element);
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "id", id);
            builder.AddAttribute(3, "role", "presentation");

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(4, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(5, "style", resolvedStyle);
            }

            builder.AddElementReferenceCapture(6, elementReference => Element = elementReference);
            builder.AddContent(7, ChildContent);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    public void Dispose()
    {
        if (hasRegisteredId)
        {
            GroupContext?.SetLabelId(null);
        }
    }
}
