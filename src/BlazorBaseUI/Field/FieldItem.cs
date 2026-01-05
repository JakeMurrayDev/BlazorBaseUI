using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Utilities.LabelableProvider;
using BlazorBaseUI.CheckboxGroup;

namespace BlazorBaseUI.Field;

public sealed class FieldItem : ComponentBase, IFieldStateSubscriber, IDisposable
{
    private const string DefaultTag = "div";

    private string? controlId;
    private string? labelId;
    private List<string> messageIds = [];
    private bool labelableNotifyPending;
    private bool isComponentRenderAs;
    private LabelableContext labelableContext = null!;
    private FieldItemContext itemContext = null!;

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
        controlId = InitialControlId ?? Guid.NewGuid().ToIdString();
        itemContext = new FieldItemContext(ResolvedDisabled);
        labelableContext = CreateLabelableContext();

        FieldContext?.Subscribe(this);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        itemContext = new FieldItemContext(ResolvedDisabled);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<LabelableContext>>(0);
        builder.AddComponentParameter(1, "Value", labelableContext);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(builder2 =>
        {
            builder2.OpenComponent<CascadingValue<FieldItemContext>>(0);
            builder2.AddComponentParameter(3, "Value", itemContext);
            builder2.AddComponentParameter(4, "IsFixed", false);
            builder2.AddComponentParameter(5, "ChildContent", (RenderFragment)RenderContent);
            builder2.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private void RenderContent(RenderTreeBuilder builder)
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

        if (state.Disabled)
        {
            builder.AddAttribute(2, "data-disabled", string.Empty);
        }

        if (state.Valid == true)
        {
            builder.AddAttribute(3, "data-valid", string.Empty);
        }
        else if (state.Valid == false)
        {
            builder.AddAttribute(4, "data-invalid", string.Empty);
        }

        if (state.Touched)
        {
            builder.AddAttribute(5, "data-touched", string.Empty);
        }

        if (state.Dirty)
        {
            builder.AddAttribute(6, "data-dirty", string.Empty);
        }

        if (state.Filled)
        {
            builder.AddAttribute(7, "data-filled", string.Empty);
        }

        if (state.Focused)
        {
            builder.AddAttribute(8, "data-focused", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(9, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(10, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddAttribute(11, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(12, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(13, elementReference => Element = elementReference);
            builder.AddContent(14, ChildContent);
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
    }

    private LabelableContext CreateLabelableContext() => new(
        ControlId: controlId,
        SetControlId: SetControlId,
        LabelId: labelId,
        SetLabelId: SetLabelId,
        MessageIds: messageIds,
        UpdateMessageIds: UpdateMessageIds);

    private void ScheduleLabelableStateHasChanged()
    {
        if (labelableNotifyPending)
            return;

        labelableNotifyPending = true;
        _ = InvokeAsync(() =>
        {
            labelableNotifyPending = false;
            labelableContext = CreateLabelableContext();
            StateHasChanged();
        });
    }

    private void SetControlId(string? id)
    {
        if (controlId == id) return;
        controlId = id;
        ScheduleLabelableStateHasChanged();
    }

    private void SetLabelId(string? id)
    {
        if (labelId == id) return;
        labelId = id;
        ScheduleLabelableStateHasChanged();
    }

    private void UpdateMessageIds(string id, bool add)
    {
        if (add)
        {
            if (messageIds.Contains(id)) return;
            messageIds = [.. messageIds, id];
        }
        else
        {
            if (!messageIds.Contains(id)) return;
            messageIds = messageIds.Where(m => m != id).ToList();
        }
        ScheduleLabelableStateHasChanged();
    }
}
