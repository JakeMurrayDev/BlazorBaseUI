using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Checkbox;

public sealed class CheckboxIndicator : ComponentBase, IDisposable
{
    private const string DefaultTag = "span";

    private bool isMounted;
    private TransitionStatus transitionStatus = TransitionStatus.Undefined;
    private CancellationTokenSource? transitionCts;
    private CheckboxIndicatorState state;
    private bool previousChecked;
    private bool previousDisabled;
    private bool previousReadOnly;
    private bool previousRequired;
    private bool previousIndeterminate;
    private bool? previousValid;
    private bool previousTouched;
    private bool previousDirty;
    private bool previousFilled;
    private bool previousFocused;
    private TransitionStatus previousTransitionStatus = TransitionStatus.Undefined;

    [CascadingParameter]
    private CheckboxRootContext? CheckboxContext { get; set; }

    [Parameter]
    public bool KeepMounted { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<CheckboxIndicatorState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<CheckboxIndicatorState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private bool Rendered => CheckboxContext?.Checked == true || CheckboxContext?.Indeterminate == true;

    private bool IsPresent => KeepMounted || isMounted || Rendered;

    protected override void OnParametersSet()
    {
        UpdateTransitionStatus();

        var rootState = CheckboxContext?.State ?? CheckboxRootState.Default;

        var stateChanged = previousChecked != rootState.Checked ||
                           previousDisabled != rootState.Disabled ||
                           previousReadOnly != rootState.ReadOnly ||
                           previousRequired != rootState.Required ||
                           previousIndeterminate != rootState.Indeterminate ||
                           previousValid != rootState.Valid ||
                           previousTouched != rootState.Touched ||
                           previousDirty != rootState.Dirty ||
                           previousFilled != rootState.Filled ||
                           previousFocused != rootState.Focused ||
                           previousTransitionStatus != transitionStatus;

        if (stateChanged)
        {
            state = CheckboxIndicatorState.FromRootState(rootState, transitionStatus);
        }

        previousChecked = rootState.Checked;
        previousDisabled = rootState.Disabled;
        previousReadOnly = rootState.ReadOnly;
        previousRequired = rootState.Required;
        previousIndeterminate = rootState.Indeterminate;
        previousValid = rootState.Valid;
        previousTouched = rootState.Touched;
        previousDirty = rootState.Dirty;
        previousFilled = rootState.Filled;
        previousFocused = rootState.Focused;
        previousTransitionStatus = transitionStatus;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!IsPresent)
        {
            return;
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var isComponent = RenderAs is not null;

        if (isComponent)
        {
            if (!typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
            {
                throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
            }
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);

        if (state.Indeterminate)
        {
            builder.AddAttribute(2, "data-indeterminate", string.Empty);
        }
        else if (state.Checked)
        {
            builder.AddAttribute(3, "data-checked", string.Empty);
        }
        else
        {
            builder.AddAttribute(4, "data-unchecked", string.Empty);
        }

        if (state.Disabled)
        {
            builder.AddAttribute(5, "data-disabled", string.Empty);
        }

        if (state.ReadOnly)
        {
            builder.AddAttribute(6, "data-readonly", string.Empty);
        }

        if (state.Required)
        {
            builder.AddAttribute(7, "data-required", string.Empty);
        }

        if (state.Valid == true)
        {
            builder.AddAttribute(8, "data-valid", string.Empty);
        }
        else if (state.Valid == false)
        {
            builder.AddAttribute(9, "data-invalid", string.Empty);
        }

        if (state.Touched)
        {
            builder.AddAttribute(10, "data-touched", string.Empty);
        }

        if (state.Dirty)
        {
            builder.AddAttribute(11, "data-dirty", string.Empty);
        }

        if (state.Filled)
        {
            builder.AddAttribute(12, "data-filled", string.Empty);
        }

        if (state.Focused)
        {
            builder.AddAttribute(13, "data-focused", string.Empty);
        }

        if (state.TransitionStatus == TransitionStatus.Starting)
        {
            builder.AddAttribute(14, "data-starting-style", string.Empty);
        }
        else if (state.TransitionStatus == TransitionStatus.Ending)
        {
            builder.AddAttribute(15, "data-ending-style", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(16, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(17, "style", resolvedStyle);
        }

        if (isComponent)
        {
            builder.AddAttribute(18, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(19, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(20, elementReference => Element = elementReference);
            builder.AddContent(21, ChildContent);
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
        var wasRendered = isMounted;
        var isRendered = Rendered;

        if (isRendered && !wasRendered)
        {
            isMounted = true;
            transitionStatus = TransitionStatus.Starting;
            ScheduleTransitionEnd();
        }
        else if (!isRendered && wasRendered)
        {
            transitionStatus = TransitionStatus.Ending;
            ScheduleUnmount();
        }
    }

    private void ScheduleTransitionEnd()
    {
        transitionCts?.Cancel();
        transitionCts = new CancellationTokenSource();
        var token = transitionCts.Token;

        _ = Task.Run(async () =>
        {
            await Task.Delay(1, token);
            if (!token.IsCancellationRequested)
            {
                transitionStatus = TransitionStatus.Undefined;
                await InvokeAsync(StateHasChanged);
            }
        }, token);
    }

    private void ScheduleUnmount()
    {
        transitionCts?.Cancel();
        transitionCts = new CancellationTokenSource();
        var token = transitionCts.Token;

        _ = Task.Run(async () =>
        {
            await Task.Delay(150, token);
            if (!token.IsCancellationRequested)
            {
                isMounted = false;
                transitionStatus = TransitionStatus.Undefined;
                await InvokeAsync(StateHasChanged);
            }
        }, token);
    }
}
