using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorBaseUI.ToggleGroup;

namespace BlazorBaseUI.Toggle;

public sealed class Toggle : ComponentBase, IAsyncDisposable
{
    private const string DefaultTag = "button";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-toggle.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool hasRendered;
    private bool isPressed;
    private bool previousPressed;
    private bool previousDisabled;
    private Orientation previousOrientation;
    private string? defaultId;
    private string resolvedValue = null!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private IToggleGroupContext? GroupContext { get; set; }

    [Parameter]
    public bool? Pressed { get; set; }

    [Parameter]
    public bool DefaultPressed { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool NativeButton { get; set; } = true;

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public EventCallback<bool> PressedChanged { get; set; }

    [Parameter]
    public EventCallback<TogglePressedChangeEventArgs> OnPressedChange { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<ToggleState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<ToggleState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    private bool IsControlled => Pressed.HasValue;

    private bool IsInGroup => GroupContext is not null;

    private bool CurrentPressed
    {
        get
        {
            if (IsInGroup)
                return GroupContext!.Value.Contains(resolvedValue);

            return IsControlled ? Pressed!.Value : isPressed;
        }
    }

    private bool ResolvedDisabled => Disabled || (GroupContext?.Disabled ?? false);

    private Orientation CurrentOrientation => GroupContext?.Orientation ?? Orientation.Horizontal;

    private ToggleState State => new(CurrentPressed, ResolvedDisabled);

    public Toggle()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        resolvedValue = Value ?? (defaultId ??= Guid.NewGuid().ToIdString());

        if (!IsInGroup && !IsControlled)
        {
            isPressed = DefaultPressed;
        }

        previousPressed = CurrentPressed;
        previousDisabled = ResolvedDisabled;
        previousOrientation = CurrentOrientation;
    }

    protected override void OnParametersSet()
    {
        var newValue = Value ?? defaultId!;
        if (newValue != resolvedValue)
        {
            GroupContext?.UnregisterToggle(this);
            resolvedValue = newValue;
        }

        if (hasRendered)
        {
            if (previousPressed != CurrentPressed)
            {
                previousPressed = CurrentPressed;
            }

            if (ResolvedDisabled != previousDisabled)
            {
                previousDisabled = ResolvedDisabled;
                _ = UpdateJsStateAsync();
            }

            if (IsInGroup && CurrentOrientation != previousOrientation)
            {
                previousOrientation = CurrentOrientation;
                _ = UpdateGroupItemOrientationAsync();
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(State));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(State));
        var attributes = BuildAttributes(State);

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
        builder.AddElementReferenceCapture(5, e => Element = e);
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;

            if (IsInGroup && Element.HasValue)
            {
                GroupContext!.RegisterToggle(
                    this,
                    Element.Value,
                    resolvedValue,
                    () => ResolvedDisabled,
                    FocusAsync);

                await InitializeGroupItemAsync();
            }

            if (!NativeButton)
            {
                await InitializeJsAsync();
            }
        }
        else if (IsInGroup && Element.HasValue)
        {
            GroupContext!.RegisterToggle(
                this,
                Element.Value,
                resolvedValue,
                () => ResolvedDisabled,
                FocusAsync);
        }
    }

    public async ValueTask DisposeAsync()
    {
        GroupContext?.UnregisterToggle(this);

        if (moduleTask.IsValueCreated)
        {
            try
            {
                var module = await moduleTask.Value;

                if (Element.HasValue)
                {
                    if (IsInGroup)
                    {
                        await module.InvokeVoidAsync("disposeGroupItem", Element.Value);
                    }

                    if (!NativeButton)
                    {
                        await module.InvokeVoidAsync("dispose", Element.Value);
                    }
                }

                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    private Dictionary<string, object> BuildAttributes(ToggleState state)
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

        attributes["aria-pressed"] = CurrentPressed;

        if (NativeButton)
        {
            attributes["type"] = "button";

            if (ResolvedDisabled)
            {
                attributes["Disabled"] = true;
            }
        }
        else
        {
            attributes["role"] = "button";

            if (ResolvedDisabled)
            {
                attributes["aria-Disabled"] = true;
                attributes["tabindex"] = -1;
            }
        }

        if (!ResolvedDisabled)
        {
            if (IsInGroup)
            {
                var isFirstEnabled = GroupContext!.IsFirstEnabledToggle(this);
                var anyPressed = GroupContext.Value.Count > 0;
                var isCurrentPressed = CurrentPressed;

                if (isCurrentPressed)
                    attributes["tabindex"] = 0;
                else if (!anyPressed && isFirstEnabled)
                    attributes["tabindex"] = 0;
                else
                    attributes["tabindex"] = -1;
            }
            else
            {
                attributes["tabindex"] = 0;
            }
        }

        attributes["onclick"] = EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync);

        if (IsInGroup)
        {
            attributes["onkeydown"] = EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync);
        }

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }

    private async Task InitializeJsAsync()
    {
        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("initialize", Element, ResolvedDisabled);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task InitializeGroupItemAsync()
    {
        try
        {
            var module = await moduleTask.Value;
            var orientationString = CurrentOrientation.ToDataAttributeString() ?? "horizontal";
            await module.InvokeVoidAsync("initializeGroupItem", Element, orientationString);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task UpdateJsStateAsync()
    {
        if (!hasRendered || NativeButton)
            return;

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("updateState", Element, ResolvedDisabled);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task UpdateGroupItemOrientationAsync()
    {
        if (!hasRendered || !IsInGroup)
            return;

        try
        {
            var module = await moduleTask.Value;
            var orientationString = CurrentOrientation.ToDataAttributeString() ?? "horizontal";
            await module.InvokeVoidAsync("updateGroupItemOrientation", Element, orientationString);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task HandleClickAsync(MouseEventArgs e)
    {
        if (ResolvedDisabled)
            return;

        var nextPressed = !CurrentPressed;

        if (IsInGroup)
        {
            await GroupContext!.SetGroupValueAsync(resolvedValue, nextPressed);
            return;
        }

        var eventArgs = new TogglePressedChangeEventArgs(nextPressed);

        if (OnPressedChange.HasDelegate)
        {
            await OnPressedChange.InvokeAsync(eventArgs);

            if (eventArgs.IsCanceled)
                return;
        }

        if (!IsControlled)
        {
            isPressed = nextPressed;
        }

        if (PressedChanged.HasDelegate)
        {
            await PressedChanged.InvokeAsync(nextPressed);
        }

        StateHasChanged();
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (!IsInGroup || ResolvedDisabled)
            return;

        var orientation = GroupContext!.Orientation;
        var isHorizontal = orientation == Orientation.Horizontal;
        var isVertical = orientation == Orientation.Vertical;

        var shouldNavigatePrevious =
            (isHorizontal && e.Key == "ArrowLeft") ||
            (isVertical && e.Key == "ArrowUp");

        var shouldNavigateNext =
            (isHorizontal && e.Key == "ArrowRight") ||
            (isVertical && e.Key == "ArrowDown");

        if (shouldNavigatePrevious)
        {
            await GroupContext.NavigateToPreviousAsync(this);
        }
        else if (shouldNavigateNext)
        {
            await GroupContext.NavigateToNextAsync(this);
        }
        else if (e.Key == "Home")
        {
            await NavigateToFirstAsync();
        }
        else if (e.Key == "End")
        {
            await NavigateToLastAsync();
        }
    }

    private async Task NavigateToFirstAsync()
    {
        if (GroupContext is null)
            return;

        await GroupContext.NavigateToFirstAsync();
    }

    private async Task NavigateToLastAsync()
    {
        if (GroupContext is null)
            return;

        await GroupContext.NavigateToLastAsync();
    }

    private async ValueTask FocusAsync()
    {
        if (!hasRendered || !Element.HasValue)
            return;

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("focus", Element.Value);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }
}
