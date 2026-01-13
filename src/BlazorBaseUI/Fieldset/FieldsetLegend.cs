using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Fieldset;

public sealed class FieldsetLegend : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "div";

    private string? defaultId;
    private FieldsetLegendState state = new(Disabled: false);
    private bool isComponentRenderAs;
    private bool disabled;

    private string ResolvedId
    {
        get
        {
            var id = AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());
            if (id != FieldsetContext?.LegendId)
            {
                FieldsetContext?.SetLegendId(id);
            }
            return id;
        }
    }

    [CascadingParameter]
    private FieldsetRootContext? FieldsetContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<FieldsetLegendState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<FieldsetLegendState, string>? StyleValue { get; set; }

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

        disabled = FieldsetContext?.Disabled ?? false;

        if (state.Disabled != disabled)
        {
            state = new FieldsetLegendState(disabled);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "id", ResolvedId);
            if (disabled)
            {
                builder.AddAttribute(3, "data-disabled", "");
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
        }
        else
        {
            builder.OpenElement(8, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(9, AdditionalAttributes);
            builder.AddAttribute(10, "id", ResolvedId);
            if (disabled)
            {
                builder.AddAttribute(11, "data-disabled", "");
            }
            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(12, "class", resolvedClass);
            }
            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(13, "style", resolvedStyle);
            }
            builder.AddElementReferenceCapture(14, elementReference => Element = elementReference);
            builder.AddContent(15, ChildContent);
            builder.CloseElement();
        }
    }

    public void Dispose()
    {
        FieldsetContext?.SetLegendId(null);
    }
}
