using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Fieldset;

public sealed class FieldsetRoot : ComponentBase
{
    private const string DefaultTag = "fieldset";

    private string? legendId;
    private FieldsetRootState state = new(Disabled: false);
    private bool isComponentRenderAs;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<FieldsetRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<FieldsetRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;

        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (state.Disabled != Disabled)
        {
            state = new FieldsetRootState(Disabled);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new FieldsetRootContext(
            LegendId: legendId,
            SetLegendId: SetLegendId,
            Disabled: Disabled);

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        builder.OpenComponent<CascadingValue<FieldsetRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(contentBuilder =>
        {
            if (isComponentRenderAs)
            {
                contentBuilder.OpenComponent(0, RenderAs!);
                contentBuilder.AddMultipleAttributes(1, AdditionalAttributes);
                if (!string.IsNullOrEmpty(legendId))
                {
                    contentBuilder.AddAttribute(2, "aria-labelledby", legendId);
                }
                if (Disabled)
                {
                    contentBuilder.AddAttribute(3, "disabled", true);
                    contentBuilder.AddAttribute(4, "data-disabled", "");
                }
                contentBuilder.AddAttribute(5, "class", resolvedClass);
                contentBuilder.AddAttribute(6, "style", resolvedStyle);
                contentBuilder.AddComponentParameter(7, "ChildContent", ChildContent);
                contentBuilder.AddComponentReferenceCapture(8, component => Element = ((IReferencableComponent)component).Element);
                contentBuilder.CloseComponent();
            }
            else
            {
                contentBuilder.OpenElement(9, !string.IsNullOrEmpty(As) ? As : DefaultTag);
                contentBuilder.AddMultipleAttributes(10, AdditionalAttributes);
                if (!string.IsNullOrEmpty(legendId))
                {
                    contentBuilder.AddAttribute(11, "aria-labelledby", legendId);
                }
                if (Disabled)
                {
                    contentBuilder.AddAttribute(12, "disabled", true);
                    contentBuilder.AddAttribute(13, "data-disabled", "");
                }
                if (!string.IsNullOrEmpty(resolvedClass))
                {
                    contentBuilder.AddAttribute(14, "class", resolvedClass);
                }
                if (!string.IsNullOrEmpty(resolvedStyle))
                {
                    contentBuilder.AddAttribute(15, "style", resolvedStyle);
                }
                contentBuilder.AddElementReferenceCapture(16, elementReference => Element = elementReference);
                contentBuilder.AddContent(17, ChildContent);
                contentBuilder.CloseElement();
            }
        }));
        builder.CloseComponent();
    }

    private void SetLegendId(string? id)
    {
        if (legendId == id) return;
        legendId = id;
        StateHasChanged();
    }
}
