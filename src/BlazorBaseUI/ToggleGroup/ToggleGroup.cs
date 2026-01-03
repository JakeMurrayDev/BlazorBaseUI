using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorBaseUI.ToggleGroup;

public sealed class ToggleGroup : ComponentBase, IAsyncDisposable
{
    private const string DefaultTag = "div";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-toggle.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool hasRendered;
    private List<string> internalValue = [];
    private ElementReference element;
    private ToggleGroupContext? groupContext;

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

    [DisallowNull]
    public ElementReference? Element => element;

    private bool IsControlled => Value is not null;

    private IReadOnlyList<string> CurrentValue => IsControlled ? Value! : internalValue;

    private ToggleGroupState State => new(Disabled, Multiple, Orientation);

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

        groupContext = CreateContext();
    }

    protected override void OnParametersSet()
    {
        groupContext?.UpdateProperties(Disabled, Orientation, LoopFocus);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var state = State;

        builder.OpenComponent<CascadingValue<IToggleGroupContext>>(0);
        builder.AddComponentParameter(1, "Value", groupContext);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(contextBuilder =>
        {
            RenderGroup(contextBuilder, state);
        }));
        builder.CloseComponent();
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
        if (moduleTask.IsValueCreated && element.Id is not null)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("disposeGroup", element);
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    private void RenderGroup(RenderTreeBuilder builder, ToggleGroupState state)
    {
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

    private Dictionary<string, object> BuildAttributes(ToggleGroupState state)
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

        attributes["role"] = "group";

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }

    private ToggleGroupContext CreateContext() => new(
        disabled: Disabled,
        orientation: Orientation,
        loopFocus: LoopFocus,
        getValue: () => CurrentValue,
        setGroupValue: SetGroupValueInternalAsync);

    private async Task InitializeJsAsync()
    {
        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("initializeGroup", element);
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
                    newGroupValue.Add(toggleValue);
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
