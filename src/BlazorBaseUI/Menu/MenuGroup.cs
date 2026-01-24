using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Menu;

public sealed class MenuGroup : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private static readonly MenuGroupState CachedState = new();

    private bool isComponentRenderAs;
    private string? labelId;
    private MenuGroupContext? groupContext;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<MenuGroupState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<MenuGroupState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        groupContext = new MenuGroupContext(SetLabelId);
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
        builder.OpenComponent<CascadingValue<IMenuGroupContext>>(0);
        builder.AddComponentParameter(1, "Value", groupContext);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)RenderContent);
        builder.CloseComponent();
    }

    private void RenderContent(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(CachedState));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(CachedState));

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "role", "group");

            if (!string.IsNullOrEmpty(labelId))
            {
                builder.AddAttribute(3, "aria-labelledby", labelId);
            }

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
            builder.AddAttribute(2, "role", "group");

            if (!string.IsNullOrEmpty(labelId))
            {
                builder.AddAttribute(3, "aria-labelledby", labelId);
            }

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

    private void SetLabelId(string? id)
    {
        if (labelId != id)
        {
            labelId = id;
            StateHasChanged();
        }
    }
}
