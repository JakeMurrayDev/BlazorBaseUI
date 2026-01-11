using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorBaseUI.ToggleGroup;

public sealed class ToggleGroup : ComponentBase, IAsyncDisposable
{
    private const string DefaultTag = "div";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-toggle.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private List<string> internalValue = [];
    private ToggleGroupContext? groupContext;
    private bool isComponentRenderAs;
    private ToggleGroupState state = ToggleGroupState.Default;

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

        groupContext?.UpdateProperties(Disabled, Orientation, LoopFocus);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<IToggleGroupContext>>(0);
        builder.AddComponentParameter(1, "Value", groupContext);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(innerBuilder =>
        {
            var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
            var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
            var orientationString = Orientation.ToDataAttributeString();

            if (isComponentRenderAs)
            {
                innerBuilder.OpenComponent(0, RenderAs!);
            }
            else
            {
                innerBuilder.OpenElement(1, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            }

            innerBuilder.AddMultipleAttributes(2, AdditionalAttributes);
            innerBuilder.AddAttribute(3, "role", "group");

            if (Disabled)
            {
                innerBuilder.AddAttribute(4, "data-disabled", string.Empty);
            }

            if (Multiple)
            {
                innerBuilder.AddAttribute(5, "data-multiple", string.Empty);
            }

            if (orientationString is not null)
            {
                innerBuilder.AddAttribute(6, "data-orientation", orientationString);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                innerBuilder.AddAttribute(7, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                innerBuilder.AddAttribute(8, "style", resolvedStyle);
            }

            if (isComponentRenderAs)
            {
                innerBuilder.AddAttribute(9, "ChildContent", ChildContent);
                innerBuilder.AddComponentReferenceCapture(10, component => { Element = ((IReferencableComponent)component).Element; });
                innerBuilder.CloseComponent();
            }
            else
            {
                innerBuilder.AddElementReferenceCapture(11, elementReference => Element = elementReference);
                innerBuilder.AddContent(12, ChildContent);
                innerBuilder.CloseElement();
            }
        }));
        builder.CloseComponent();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
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
        Disabled: Disabled,
        Orientation: Orientation,
        LoopFocus: LoopFocus,
        GetValue: () => CurrentValue,
        SetGroupValue: SetGroupValueInternalAsync);

    private async Task InitializeJsAsync()
    {
        if (!Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("initializeGroup", Element.Value);
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
