using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Radio;

public sealed class RadioIndicator : ComponentBase, IDisposable
{
    private const string DefaultTag = "span";

    private bool isMounted;
    private bool previousRendered;
    private TransitionStatus transitionStatus = TransitionStatus.Undefined;
    private CancellationTokenSource? transitionCts;
    private RadioIndicatorState? cachedState;

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

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    private bool Rendered => RadioContext?.Checked == true;

    private bool IsPresent => KeepMounted || isMounted || Rendered;

    private RadioIndicatorState State
    {
        get
        {
            var newState = new RadioIndicatorState(
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

            if (cachedState is null || !StatesEqual(cachedState, newState))
            {
                cachedState = newState;
            }

            return cachedState;
        }
    }

    protected override void OnParametersSet()
    {
        UpdateTransitionStatus();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!IsPresent)
            return;

        var state = State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (RenderAs is not null)
        {
            builder.OpenComponent(0, RenderAs);
            builder.AddMultipleAttributes(1, BuildAttributes(state, resolvedClass, resolvedStyle));
            builder.AddComponentParameter(2, "ChildContent", ChildContent);
            builder.CloseComponent();
            return;
        }

        var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
        builder.OpenElement(3, tag);
        builder.AddMultipleAttributes(4, BuildAttributes(state, resolvedClass, resolvedStyle));
        builder.AddElementReferenceCapture(5, e => Element = e);
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
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

        _ = TransitionEndAsync(token);
    }

    private async Task TransitionEndAsync(CancellationToken token)
    {
        await Task.Yield();
        if (token.IsCancellationRequested)
            return;

        transitionStatus = TransitionStatus.Undefined;
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
        await InvokeAsync(StateHasChanged);
    }

    private Dictionary<string, object> BuildAttributes(RadioIndicatorState state, string? resolvedClass, string? resolvedStyle)
    {
        var attributes = new Dictionary<string, object>();

        if (AdditionalAttributes is not null)
        {
            foreach (var attr in AdditionalAttributes)
            {
                if (attr.Key is not "class" and not "style")
                    attributes[attr.Key] = attr.Value;
            }
        }

        state.WriteDataAttributes(attributes);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        return attributes;
    }

    private static bool StatesEqual(RadioIndicatorState a, RadioIndicatorState b) =>
        a.Checked == b.Checked &&
        a.Disabled == b.Disabled &&
        a.ReadOnly == b.ReadOnly &&
        a.Required == b.Required &&
        a.Valid == b.Valid &&
        a.Touched == b.Touched &&
        a.Dirty == b.Dirty &&
        a.Filled == b.Filled &&
        a.Focused == b.Focused &&
        a.TransitionStatus == b.TransitionStatus;
}
