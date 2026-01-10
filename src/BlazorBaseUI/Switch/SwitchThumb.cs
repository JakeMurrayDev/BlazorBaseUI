using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Switch;

public sealed class SwitchThumb : ComponentBase
{
    private const string DefaultTag = "span";

    private bool isComponentRenderAs;
    private SwitchRootState state = SwitchRootState.Default;

    [CascadingParameter]
    private SwitchRootContext? SwitchContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<SwitchRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<SwitchRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;

        var newState = SwitchContext?.State ?? SwitchRootState.Default;
        if (state != newState)
        {
            state = newState;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);

        if (state.Checked)
        {
            builder.AddAttribute(2, "data-checked", string.Empty);
        }
        else
        {
            builder.AddAttribute(3, "data-unchecked", string.Empty);
        }

        if (state.Disabled)
        {
            builder.AddAttribute(4, "data-disabled", string.Empty);
        }

        if (state.ReadOnly)
        {
            builder.AddAttribute(5, "data-readonly", string.Empty);
        }

        if (state.Required)
        {
            builder.AddAttribute(6, "data-required", string.Empty);
        }

        if (state.Valid == true)
        {
            builder.AddAttribute(7, "data-valid", string.Empty);
        }
        else if (state.Valid == false)
        {
            builder.AddAttribute(8, "data-invalid", string.Empty);
        }

        if (state.Touched)
        {
            builder.AddAttribute(9, "data-touched", string.Empty);
        }

        if (state.Dirty)
        {
            builder.AddAttribute(10, "data-dirty", string.Empty);
        }

        if (state.Filled)
        {
            builder.AddAttribute(11, "data-filled", string.Empty);
        }

        if (state.Focused)
        {
            builder.AddAttribute(12, "data-focused", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(13, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(14, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(15, "ChildContent", ChildContent);
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(15, elementReference => Element = elementReference);
            builder.AddContent(16, ChildContent);
            builder.CloseElement();
        }
    }
}
