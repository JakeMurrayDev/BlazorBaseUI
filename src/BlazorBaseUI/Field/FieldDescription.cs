using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Utilities.LabelableProvider;

namespace BlazorBaseUI.Field;

public sealed class FieldDescription : ComponentBase, IFieldStateSubscriber, IDisposable
{
    private const string DefaultTag = "p";

    private string? defaultId;
    private bool isComponentRenderAs;

    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    [CascadingParameter]
    private LabelableContext? LabelableContext { get; set; }

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

    private FieldRootState State => FieldContext?.State ?? FieldRootState.Default;

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    protected override void OnInitialized()
    {
        LabelableContext?.UpdateMessageIds.Invoke(ResolvedId, true);
        FieldContext?.Subscribe(this);
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
        var state = State;
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
        builder.AddAttribute(2, "id", ResolvedId);

        if (state.Disabled)
        {
            builder.AddAttribute(3, "data-disabled", string.Empty);
        }

        if (state.Valid == true)
        {
            builder.AddAttribute(4, "data-valid", string.Empty);
        }
        else if (state.Valid == false)
        {
            builder.AddAttribute(5, "data-invalid", string.Empty);
        }

        if (state.Touched)
        {
            builder.AddAttribute(6, "data-touched", string.Empty);
        }

        if (state.Dirty)
        {
            builder.AddAttribute(7, "data-dirty", string.Empty);
        }

        if (state.Filled)
        {
            builder.AddAttribute(8, "data-filled", string.Empty);
        }

        if (state.Focused)
        {
            builder.AddAttribute(9, "data-focused", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(10, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(11, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddAttribute(12, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(13, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(14, elementReference => Element = elementReference);
            builder.AddContent(15, ChildContent);
            builder.CloseElement();
        }
    }

    void IFieldStateSubscriber.NotifyStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        FieldContext?.Unsubscribe(this);
        LabelableContext?.UpdateMessageIds.Invoke(ResolvedId, false);
    }
}
