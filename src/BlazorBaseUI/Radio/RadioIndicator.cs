using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Radio;

public sealed class RadioIndicator : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "span";

    private bool isComponentRenderAs;
    private bool isMounted;
    private bool previousRendered;
    private TransitionStatus transitionStatus = TransitionStatus.Undefined;
    private CancellationTokenSource? transitionCts;
    private RadioIndicatorState state = new(false, false, false, false, null, false, false, false, false, TransitionStatus.Undefined);
    private bool stateDirty = true;

    [CascadingParameter]
    private RadioRootContext? RadioContext { get; set; }

    [Parameter]
    public bool KeepMounted { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<RadioIndicatorState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<RadioIndicatorState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private bool Rendered => RadioContext?.Checked == true;

    private bool IsPresent => KeepMounted || isMounted || Rendered;

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;

        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var newChecked = RadioContext?.Checked ?? false;
        var newDisabled = RadioContext?.Disabled ?? false;
        var newReadOnly = RadioContext?.ReadOnly ?? false;
        var newRequired = RadioContext?.Required ?? false;
        var newValid = RadioContext?.State.Valid;
        var newTouched = RadioContext?.State.Touched ?? false;
        var newDirty = RadioContext?.State.Dirty ?? false;
        var newFilled = RadioContext?.State.Filled ?? false;
        var newFocused = RadioContext?.State.Focused ?? false;

        if (state.Checked != newChecked ||
            state.Disabled != newDisabled ||
            state.ReadOnly != newReadOnly ||
            state.Required != newRequired ||
            state.Valid != newValid ||
            state.Touched != newTouched ||
            state.Dirty != newDirty ||
            state.Filled != newFilled ||
            state.Focused != newFocused ||
            state.TransitionStatus != transitionStatus)
        {
            stateDirty = true;
        }

        UpdateTransitionStatus();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!IsPresent)
            return;

        if (stateDirty)
        {
            state = new RadioIndicatorState(
                RadioContext?.Checked ?? false,
                RadioContext?.Disabled ?? false,
                RadioContext?.ReadOnly ?? false,
                RadioContext?.Required ?? false,
                RadioContext?.State.Valid,
                RadioContext?.State.Touched ?? false,
                RadioContext?.State.Dirty ?? false,
                RadioContext?.State.Filled ?? false,
                RadioContext?.State.Focused ?? false,
                transitionStatus);
            stateDirty = false;
        }

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
            builder.AddAttribute(2, "data-checked", string.Empty);
        else
            builder.AddAttribute(3, "data-unchecked", string.Empty);

        if (state.Disabled)
            builder.AddAttribute(4, "data-disabled", string.Empty);

        if (state.ReadOnly)
            builder.AddAttribute(5, "data-readonly", string.Empty);

        if (state.Required)
            builder.AddAttribute(6, "data-required", string.Empty);

        if (state.Valid == true)
            builder.AddAttribute(7, "data-valid", string.Empty);
        else if (state.Valid == false)
            builder.AddAttribute(8, "data-invalid", string.Empty);

        if (state.Touched)
            builder.AddAttribute(9, "data-touched", string.Empty);

        if (state.Dirty)
            builder.AddAttribute(10, "data-dirty", string.Empty);

        if (state.Filled)
            builder.AddAttribute(11, "data-filled", string.Empty);

        if (state.Focused)
            builder.AddAttribute(12, "data-focused", string.Empty);

        if (state.TransitionStatus == TransitionStatus.Starting)
            builder.AddAttribute(13, "data-starting-style", string.Empty);
        else if (state.TransitionStatus == TransitionStatus.Ending)
            builder.AddAttribute(14, "data-ending-style", string.Empty);

        if (!string.IsNullOrEmpty(resolvedClass))
            builder.AddAttribute(15, "class", resolvedClass);

        if (!string.IsNullOrEmpty(resolvedStyle))
            builder.AddAttribute(16, "style", resolvedStyle);

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(17, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(18, component => Element = ((IReferencableComponent)component).Element);
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(19, elementReference => Element = elementReference);
            builder.AddContent(20, ChildContent);
            builder.CloseElement();
        }
    }

    public void Dispose()
    {
        transitionCts?.Cancel();
        transitionCts?.Dispose();
    }

    private void UpdateTransitionStatus()
    {
        var wasRendered = previousRendered;
        var isRendered = Rendered;
        previousRendered = isRendered;

        if (isRendered && !wasRendered)
        {
            isMounted = true;
            transitionStatus = TransitionStatus.Starting;
            stateDirty = true;
            ScheduleTransitionEnd();
        }
        else if (!isRendered && wasRendered)
        {
            transitionStatus = TransitionStatus.Ending;
            stateDirty = true;
            ScheduleUnmount();
        }
    }

    private void ScheduleTransitionEnd()
    {
        transitionCts?.Cancel();
        transitionCts = new CancellationTokenSource();
        var token = transitionCts.Token;

        _ = TransitionEndAsync(token);
    }

    private async Task TransitionEndAsync(CancellationToken token)
    {
        await Task.Yield();
        if (token.IsCancellationRequested)
            return;

        transitionStatus = TransitionStatus.Undefined;
        stateDirty = true;
        await InvokeAsync(StateHasChanged);
    }

    private void ScheduleUnmount()
    {
        transitionCts?.Cancel();
        transitionCts = new CancellationTokenSource();
        var token = transitionCts.Token;

        _ = UnmountAsync(token);
    }

    private async Task UnmountAsync(CancellationToken token)
    {
        await Task.Delay(150, token);
        if (token.IsCancellationRequested)
            return;

        isMounted = false;
        transitionStatus = TransitionStatus.Undefined;
        stateDirty = true;
        await InvokeAsync(StateHasChanged);
    }
}
