using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Toolbar;

public sealed class ToolbarGroup : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;
    private ToolbarRootState state = default!;
    private ToolbarGroupContext context = default!;

    [CascadingParameter]
    private ToolbarRootContext? RootContext { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<ToolbarRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<ToolbarRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnParametersSet()
    {
        if (RootContext is null)
        {
            throw new InvalidOperationException("ToolbarGroup must be placed within a ToolbarRoot.");
        }

        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var disabled = RootContext.Disabled || Disabled;
        var orientation = RootContext.Orientation;

        if (state is null || state.Disabled != disabled || state.Orientation != orientation)
        {
            state = new ToolbarRootState(disabled, orientation);
            context = new ToolbarGroupContext(disabled);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var orientationString = state.Orientation.ToDataAttributeString();

        builder.OpenComponent<CascadingValue<ToolbarGroupContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(childBuilder =>
        {
            if (isComponentRenderAs)
            {
                childBuilder.OpenRegion(0);
                childBuilder.OpenComponent(0, RenderAs!);
                childBuilder.AddMultipleAttributes(1, AdditionalAttributes);
                childBuilder.AddAttribute(2, "role", "group");
                childBuilder.AddAttribute(3, "data-orientation", orientationString);

                if (state.Disabled)
                {
                    childBuilder.AddAttribute(4, "data-disabled", "");
                }

                if (!string.IsNullOrEmpty(resolvedClass))
                {
                    childBuilder.AddAttribute(5, "class", resolvedClass);
                }

                if (!string.IsNullOrEmpty(resolvedStyle))
                {
                    childBuilder.AddAttribute(6, "style", resolvedStyle);
                }

                childBuilder.AddComponentParameter(7, "ChildContent", ChildContent);
                childBuilder.AddComponentReferenceCapture(8, component => { Element = ((IReferencableComponent)component).Element; });
                childBuilder.CloseComponent();
                childBuilder.CloseRegion();
            }
            else
            {
                childBuilder.OpenRegion(1);
                childBuilder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
                childBuilder.AddMultipleAttributes(1, AdditionalAttributes);
                childBuilder.AddAttribute(2, "role", "group");
                childBuilder.AddAttribute(3, "data-orientation", orientationString);

                if (state.Disabled)
                {
                    childBuilder.AddAttribute(4, "data-disabled", "");
                }

                if (!string.IsNullOrEmpty(resolvedClass))
                {
                    childBuilder.AddAttribute(5, "class", resolvedClass);
                }

                if (!string.IsNullOrEmpty(resolvedStyle))
                {
                    childBuilder.AddAttribute(6, "style", resolvedStyle);
                }

                childBuilder.AddElementReferenceCapture(7, elementReference => Element = elementReference);
                childBuilder.AddContent(8, ChildContent);
                childBuilder.CloseElement();
                childBuilder.CloseRegion();
            }
        }));
        builder.CloseComponent();
    }
}
