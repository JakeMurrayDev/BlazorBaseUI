using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Radio;

public sealed class RadioIndicator : ComponentBase, IDisposable
{
    private const string DefaultTag = "span";

    private bool isMounted;
    private TransitionStatus transitionStatus = TransitionStatus.Undefined;
    private ElementReference element;
    private CancellationTokenSource? transitionCts;

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
    public ElementReference? Element => element;

    private bool Rendered => RadioContext?.Checked == true;

    private bool IsPresent => KeepMounted || isMounted || Rendered;

    private RadioIndicatorState State => new(
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
        var attributes = BuildAttributes(state);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        if (RenderAs is not null)
        {
            builder.OpenComponent(0, RenderAs);
            builder.AddMultipleAttributes(1, attributes);
            builder.AddComponentParameter(2, "ChildContent", ChildContent);
            builder.CloseComponent();
            return;
        }

        var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
        builder.OpenElement(3, tag);
        builder.AddMultipleAttributes(4, attributes);
        builder.AddElementReferenceCapture(5, e => element = e);
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

    private Dictionary<string, object> BuildAttributes(RadioIndicatorState state)
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

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }
}
