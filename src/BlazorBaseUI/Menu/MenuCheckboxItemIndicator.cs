using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Menu;

public sealed class MenuCheckboxItemIndicator : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "span";

    private bool isMounted;
    private bool isComponentRenderAs;
    private TransitionStatus transitionStatus = TransitionStatus.None;
    private CancellationTokenSource? transitionCts;
    private MenuCheckboxItemIndicatorState state;
    private bool previousChecked;
    private bool previousDisabled;
    private bool previousHighlighted;
    private TransitionStatus previousTransitionStatus = TransitionStatus.None;

    private bool Rendered => ItemContext?.Checked == true;

    private bool IsPresent => KeepMounted || isMounted || Rendered;

    [CascadingParameter]
    private MenuCheckboxItemContext? ItemContext { get; set; }

    [Parameter]
    public bool KeepMounted { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<MenuCheckboxItemIndicatorState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<MenuCheckboxItemIndicatorState, string>? StyleValue { get; set; }

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

        UpdateTransitionStatus();

        var itemChecked = ItemContext?.Checked ?? false;
        var itemDisabled = ItemContext?.Disabled ?? false;
        var itemHighlighted = ItemContext?.Highlighted ?? false;

        var stateChanged = previousChecked != itemChecked ||
                           previousDisabled != itemDisabled ||
                           previousHighlighted != itemHighlighted ||
                           previousTransitionStatus != transitionStatus;

        if (stateChanged)
        {
            state = new MenuCheckboxItemIndicatorState(
                Checked: itemChecked,
                Disabled: itemDisabled,
                Highlighted: itemHighlighted,
                TransitionStatus: transitionStatus);
        }

        previousChecked = itemChecked;
        previousDisabled = itemDisabled;
        previousHighlighted = itemHighlighted;
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

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "aria-hidden", "true");

            if (state.Checked)
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

            if (state.Highlighted)
            {
                builder.AddAttribute(6, "data-highlighted", string.Empty);
            }

            if (state.TransitionStatus == TransitionStatus.Starting)
            {
                builder.AddAttribute(7, "data-starting-style", string.Empty);
            }
            else if (state.TransitionStatus == TransitionStatus.Ending)
            {
                builder.AddAttribute(8, "data-ending-style", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(9, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(10, "style", resolvedStyle);
            }

            builder.AddComponentParameter(11, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(12, component => Element = ((IReferencableComponent)component).Element);
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "aria-hidden", "true");

            if (state.Checked)
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

            if (state.Highlighted)
            {
                builder.AddAttribute(6, "data-highlighted", string.Empty);
            }

            if (state.TransitionStatus == TransitionStatus.Starting)
            {
                builder.AddAttribute(7, "data-starting-style", string.Empty);
            }
            else if (state.TransitionStatus == TransitionStatus.Ending)
            {
                builder.AddAttribute(8, "data-ending-style", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(9, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(10, "style", resolvedStyle);
            }

            builder.AddElementReferenceCapture(11, elementReference => Element = elementReference);
            builder.AddContent(12, ChildContent);
            builder.CloseElement();
            builder.CloseRegion();
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
            try
            {
                await Task.Delay(1, token);
                if (!token.IsCancellationRequested)
                {
                    transitionStatus = TransitionStatus.None;
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await DispatchExceptionAsync(ex);
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
            try
            {
                await Task.Delay(150, token);
                if (!token.IsCancellationRequested)
                {
                    isMounted = false;
                    transitionStatus = TransitionStatus.None;
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await DispatchExceptionAsync(ex);
            }
        }, token);
    }
}
