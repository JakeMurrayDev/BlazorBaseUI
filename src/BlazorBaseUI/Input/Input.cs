using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Field;

namespace BlazorBaseUI.Input;

public sealed class Input : ComponentBase, IReferencableComponent
{
    private FieldControl<string>? fieldControlRef;
    private Func<FieldRootState, string>? wrappedClassValue;
    private Func<FieldRootState, string>? wrappedStyleValue;

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public Expression<Func<string>>? ValueExpression { get; set; }

    [Parameter]
    public string? DefaultValue { get; set; }

    [Parameter]
    public string? DisplayName { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<InputState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<InputState, string>? StyleValue { get; set; }

    public ElementReference? Element => fieldControlRef?.Element;

    protected override void OnParametersSet()
    {
        wrappedClassValue = ClassValue is not null
            ? state => ClassValue(InputState.FromFieldRootState(state))
            : null;

        wrappedStyleValue = StyleValue is not null
            ? state => StyleValue(InputState.FromFieldRootState(state))
            : null;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<FieldControl<string>>(0);
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "Value", Value);
        builder.AddAttribute(3, "ValueChanged", ValueChanged);
        builder.AddAttribute(4, "ValueExpression", ValueExpression);
        builder.AddAttribute(5, "DefaultValue", DefaultValue);
        builder.AddAttribute(6, "DisplayName", DisplayName);
        builder.AddAttribute(7, "Name", Name);
        builder.AddAttribute(8, "Disabled", Disabled);
        builder.AddAttribute(9, "As", As);
        builder.AddAttribute(10, "RenderAs", RenderAs);
        builder.AddAttribute(11, "ClassValue", wrappedClassValue);
        builder.AddAttribute(12, "StyleValue", wrappedStyleValue);
        builder.AddComponentReferenceCapture(13, component => fieldControlRef = (FieldControl<string>)component);
        builder.CloseComponent();
    }
}
