using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Utilities.LabelableProvider;
using BlazorBaseUI.CheckboxGroup;

namespace BlazorBaseUI.Field;

public sealed class FieldItem : ComponentBase, IFieldStateSubscriber, IDisposable
{
    private const string DefaultTag = "div";

    private Dictionary<string, object>? cachedAttributes;
    private FieldRootState lastAttributeState;

    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    [CascadingParameter]
    private CheckboxGroupContext? CheckboxGroupContext { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<FieldRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<FieldRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private bool ResolvedDisabled => (FieldContext?.Disabled ?? false) || Disabled;

    private FieldRootState State => FieldContext?.State ?? FieldRootState.Default;

    private bool HasParentCheckbox => CheckboxGroupContext?.AllValues is not null;

    private string? InitialControlId => HasParentCheckbox ? CheckboxGroupContext?.Parent?.Id : null;

    protected override void OnInitialized()
    {
        FieldContext?.Subscribe(this);
    }

    protected override void OnParametersSet()
    {
        cachedAttributes = null;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var itemContext = new FieldItemContext(ResolvedDisabled);

        builder.OpenComponent<LabelableProvider>(0);
        builder.AddComponentParameter(1, "InitialControlId", InitialControlId);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(labelableBuilder =>
        {
            labelableBuilder.OpenComponent<CascadingValue<FieldItemContext>>(3);
            labelableBuilder.AddComponentParameter(4, "Value", itemContext);
            labelableBuilder.AddComponentParameter(5, "IsFixed", false);
            labelableBuilder.AddComponentParameter(6, "ChildContent", (RenderFragment)(RenderItem));
            labelableBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }

    void IFieldStateSubscriber.NotifyStateChanged()
    {
        cachedAttributes = null;
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        FieldContext?.Unsubscribe(this);
    }

    private void RenderItem(RenderTreeBuilder builder)
    {
        var state = State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var attributes = GetOrBuildAttributes(state);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        else
            attributes.Remove("class");

        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;
        else
            attributes.Remove("style");

        if (RenderAs is not null)
        {
            if (!typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
            {
                throw new InvalidOperationException($"Type {RenderAs.Name} must implement IReferencableComponent.");
            }
            builder.OpenComponent(0, RenderAs);
            builder.AddMultipleAttributes(1, attributes);
            builder.AddComponentParameter(2, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(3, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
            return;
        }

        var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
        builder.OpenElement(3, tag);
        builder.AddMultipleAttributes(4, attributes);
        builder.AddElementReferenceCapture(5, e => Element = e);
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
    }

    private Dictionary<string, object> GetOrBuildAttributes(FieldRootState state)
    {
        if (cachedAttributes is not null && lastAttributeState == state)
            return cachedAttributes;

        cachedAttributes = BuildAttributes(state);
        lastAttributeState = state;
        return cachedAttributes;
    }

    private Dictionary<string, object> BuildAttributes(FieldRootState state)
    {
        var dataAttrs = state.GetDataAttributes();
        var additionalCount = AdditionalAttributes?.Count ?? 0;
        var attributes = new Dictionary<string, object>(dataAttrs.Count + additionalCount);

        if (AdditionalAttributes is not null)
        {
            foreach (var attr in AdditionalAttributes)
            {
                if (attr.Key is not "class" and not "style")
                    attributes[attr.Key] = attr.Value;
            }
        }

        foreach (var dataAttr in dataAttrs)
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }
}
