using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorBaseUI.ToggleGroup;

public sealed class ToggleGroup : ComponentBase, IReferencableComponent, IAsyncDisposable
{
    private const string DefaultTag = "div";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-toggle.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private List<string> internalValue = [];
    private ToggleGroupContext? groupContext;
    private bool hasRendered;
    private bool isComponentRenderAs;
    private ToggleGroupState state = ToggleGroupState.Default;
    private Orientation previousOrientation;
    private bool previousLoopFocus;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [Parameter]
    public IReadOnlyList<string>? Value { get; set; }

    [Parameter]
    public IReadOnlyList<string>? DefaultValue { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    [Parameter]
    public bool LoopFocus { get; set; } = true;

    [Parameter]
    public bool Multiple { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyList<string>> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<ToggleGroupValueChangeEventArgs> OnValueChange { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<ToggleGroupState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<ToggleGroupState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private bool IsControlled => Value is not null;

    private IReadOnlyList<string> CurrentValue => IsControlled ? Value! : internalValue;

    public ToggleGroup()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        if (!IsControlled && DefaultValue is not null)
        {
            internalValue = [.. DefaultValue];
        }

        previousOrientation = Orientation;
        previousLoopFocus = LoopFocus;

        groupContext = CreateContext();
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (state.Disabled != Disabled || state.Multiple != Multiple || state.Orientation != Orientation)
        {
            state = new ToggleGroupState(Disabled, Multiple, Orientation);
        }

        if (groupContext is not null)
        {
            groupContext.Disabled = Disabled;
            groupContext.Orientation = Orientation;
            groupContext.LoopFocus = LoopFocus;
        }

        if (hasRendered && (Orientation != previousOrientation || LoopFocus != previousLoopFocus))
        {
            previousOrientation = Orientation;
            previousLoopFocus = LoopFocus;
            _ = UpdateJsStateAsync();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<IToggleGroupContext>>(0);
        builder.AddComponentParameter(1, "Value", groupContext);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)RenderContent);
        builder.CloseComponent();
    }

    private void RenderContent(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var orientationString = Orientation.ToDataAttributeString();

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "role", "group");

            if (Disabled)
            {
                builder.AddAttribute(3, "data-disabled", string.Empty);
            }

            if (Multiple)
            {
                builder.AddAttribute(4, "data-multiple", string.Empty);
            }

            if (orientationString is not null)
            {
                builder.AddAttribute(5, "data-orientation", orientationString);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(6, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(7, "style", resolvedStyle);
            }

            builder.AddAttribute(8, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(9, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "role", "group");

            if (Disabled)
            {
                builder.AddAttribute(3, "data-disabled", string.Empty);
            }

            if (Multiple)
            {
                builder.AddAttribute(4, "data-multiple", string.Empty);
            }

            if (orientationString is not null)
            {
                builder.AddAttribute(5, "data-orientation", orientationString);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(6, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(7, "style", resolvedStyle);
            }

            builder.AddContent(8, ChildContent);
            builder.AddElementReferenceCapture(9, elementReference => Element = elementReference);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            await InitializeJsAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated && Element.HasValue)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("disposeGroup", Element.Value);
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    private ToggleGroupContext CreateContext() => new(
        disabled: Disabled,
        orientation: Orientation,
        loopFocus: LoopFocus,
        getValue: () => CurrentValue,
        setGroupValue: SetGroupValueInternalAsync,
        getGroupElement: () => Element);

    private async Task InitializeJsAsync()
    {
        if (!Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            var orientationString = Orientation.ToDataAttributeString() ?? "horizontal";
            await module.InvokeVoidAsync("initializeGroup", Element.Value, orientationString, LoopFocus);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task UpdateJsStateAsync()
    {
        if (!hasRendered || !Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            var orientationString = Orientation.ToDataAttributeString() ?? "horizontal";
            await module.InvokeVoidAsync("updateGroup", Element.Value, orientationString, LoopFocus);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task SetGroupValueInternalAsync(string toggleValue, bool nextPressed)
    {
        List<string> newGroupValue;

        if (Multiple)
        {
            newGroupValue = [.. CurrentValue];
            if (nextPressed)
            {
                if (!newGroupValue.Contains(toggleValue))
                {
                    newGroupValue.Add(toggleValue);
                }
            }
            else
            {
                newGroupValue.Remove(toggleValue);
            }
        }
        else
        {
            newGroupValue = nextPressed ? [toggleValue] : [];
        }

        var eventArgs = new ToggleGroupValueChangeEventArgs(newGroupValue);

        if (OnValueChange.HasDelegate)
        {
            await OnValueChange.InvokeAsync(eventArgs);

            if (eventArgs.IsCanceled)
            {
                StateHasChanged();
                return;
            }
        }

        if (!IsControlled)
        {
            internalValue = newGroupValue;
        }

        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(newGroupValue);
        }

        StateHasChanged();
    }
}
