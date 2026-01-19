using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorBaseUI.ToggleGroup;

namespace BlazorBaseUI.Toggle;

public sealed class Toggle : ComponentBase, IReferencableComponent, IAsyncDisposable
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
    private bool isComponentRenderAs;
    private ToggleState state = ToggleState.Default;
    private EventCallback<MouseEventArgs> cachedOnClick;
    private EventCallback<KeyboardEventArgs> cachedOnKeyDown;

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

    public Toggle()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        cachedOnClick = EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync);
        cachedOnKeyDown = EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync);

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
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var newValue = Value ?? defaultId!;
        if (newValue != resolvedValue)
        {
            resolvedValue = newValue;
        }

        var currentPressed = CurrentPressed;
        var resolvedDisabled = ResolvedDisabled;

        if (state.Pressed != currentPressed || state.Disabled != resolvedDisabled)
        {
            state = new ToggleState(currentPressed, resolvedDisabled);
        }

        if (hasRendered)
        {
            if (previousPressed != currentPressed)
            {
                previousPressed = currentPressed;
            }

            if (resolvedDisabled != previousDisabled)
            {
                previousDisabled = resolvedDisabled;
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
        var currentPressed = CurrentPressed;
        var resolvedDisabled = ResolvedDisabled;

        if (state.Pressed != currentPressed || state.Disabled != resolvedDisabled)
        {
            state = new ToggleState(currentPressed, resolvedDisabled);
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        int? groupTabIndex = null;
        if (!resolvedDisabled && IsInGroup)
        {
            var anyPressed = GroupContext!.Value.Count > 0;
            groupTabIndex = currentPressed ? 0 : (!anyPressed ? 0 : -1);
        }

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "aria-pressed", currentPressed ? "true" : "false");

            if (NativeButton)
            {
                builder.AddAttribute(3, "type", "button");
                if (resolvedDisabled)
                {
                    builder.AddAttribute(4, "disabled", true);
                }
                else if (IsInGroup)
                {
                    builder.AddAttribute(5, "tabindex", groupTabIndex!.Value);
                }
                else
                {
                    builder.AddAttribute(6, "tabindex", 0);
                }
            }
            else
            {
                builder.AddAttribute(7, "role", "button");
                if (resolvedDisabled)
                {
                    builder.AddAttribute(8, "aria-disabled", "true");
                    builder.AddAttribute(9, "tabindex", -1);
                }
                else if (IsInGroup)
                {
                    builder.AddAttribute(10, "tabindex", groupTabIndex!.Value);
                }
                else
                {
                    builder.AddAttribute(11, "tabindex", 0);
                }
            }

            builder.AddAttribute(12, "onclick", cachedOnClick);

            if (IsInGroup)
            {
                builder.AddAttribute(13, "onkeydown", cachedOnKeyDown);
            }

            if (currentPressed)
            {
                builder.AddAttribute(14, "data-pressed", string.Empty);
            }

            if (resolvedDisabled)
            {
                builder.AddAttribute(15, "data-disabled", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(16, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(17, "style", resolvedStyle);
            }

            builder.AddAttribute(18, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(19, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "aria-pressed", currentPressed ? "true" : "false");

            if (NativeButton)
            {
                builder.AddAttribute(3, "type", "button");
                if (resolvedDisabled)
                {
                    builder.AddAttribute(4, "disabled", true);
                }
                else if (IsInGroup)
                {
                    builder.AddAttribute(5, "tabindex", groupTabIndex!.Value);
                }
                else
                {
                    builder.AddAttribute(6, "tabindex", 0);
                }
            }
            else
            {
                builder.AddAttribute(7, "role", "button");
                if (resolvedDisabled)
                {
                    builder.AddAttribute(8, "aria-disabled", "true");
                    builder.AddAttribute(9, "tabindex", -1);
                }
                else if (IsInGroup)
                {
                    builder.AddAttribute(10, "tabindex", groupTabIndex!.Value);
                }
                else
                {
                    builder.AddAttribute(11, "tabindex", 0);
                }
            }

            builder.AddAttribute(12, "onclick", cachedOnClick);

            if (IsInGroup)
            {
                builder.AddAttribute(13, "onkeydown", cachedOnKeyDown);
            }

            if (currentPressed)
            {
                builder.AddAttribute(14, "data-pressed", string.Empty);
            }

            if (resolvedDisabled)
            {
                builder.AddAttribute(15, "data-disabled", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(16, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(17, "style", resolvedStyle);
            }

            builder.AddContent(18, ChildContent);
            builder.AddElementReferenceCapture(19, elementReference => Element = elementReference);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;

            if (IsInGroup && Element.HasValue)
            {
                await InitializeGroupItemAsync();
                await RegisterWithGroupAsync();
            }

            if (!NativeButton)
            {
                await InitializeJsAsync();
            }
        }
        else if (IsInGroup && Element.HasValue)
        {
            await RegisterWithGroupAsync();
        }
    }

    private async Task RegisterWithGroupAsync()
    {
        if (!IsInGroup || !Element.HasValue || !GroupContext!.GroupElement.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("registerToggle", GroupContext.GroupElement.Value, Element.Value, resolvedValue);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task UnregisterFromGroupAsync()
    {
        if (!IsInGroup || !Element.HasValue || !GroupContext!.GroupElement.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("unregisterToggle", GroupContext.GroupElement.Value, Element.Value);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated && Element.HasValue)
        {
            try
            {
                await UnregisterFromGroupAsync();
                var module = await moduleTask.Value;

                if (IsInGroup)
                {
                    await module.InvokeVoidAsync("disposeGroupItem", Element.Value);
                }

                if (!NativeButton)
                {
                    await module.InvokeVoidAsync("dispose", Element.Value);
                }

                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    private async Task InitializeJsAsync()
    {
        if (!Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("initialize", Element.Value, ResolvedDisabled);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task InitializeGroupItemAsync()
    {
        if (!Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            var orientationString = CurrentOrientation.ToDataAttributeString() ?? "horizontal";
            await module.InvokeVoidAsync("initializeGroupItem", Element.Value, orientationString);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task UpdateJsStateAsync()
    {
        if (!hasRendered || NativeButton || !Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("updateState", Element.Value, ResolvedDisabled);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task UpdateGroupItemOrientationAsync()
    {
        if (!hasRendered || !IsInGroup || !Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            var orientationString = CurrentOrientation.ToDataAttributeString() ?? "horizontal";
            await module.InvokeVoidAsync("updateGroupItemOrientation", Element.Value, orientationString);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task HandleClickAsync(MouseEventArgs e)
    {
        if (ResolvedDisabled)
        {
            return;
        }

        var nextPressed = !CurrentPressed;

        if (IsInGroup)
        {
            await GroupContext!.SetGroupValueAsync(resolvedValue, nextPressed);
            await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
            return;
        }

        var eventArgs = new TogglePressedChangeEventArgs(nextPressed);

        if (OnPressedChange.HasDelegate)
        {
            await OnPressedChange.InvokeAsync(eventArgs);

            if (eventArgs.IsCanceled)
            {
                return;
            }
        }

        if (!IsControlled)
        {
            isPressed = nextPressed;
            state = new ToggleState(isPressed, ResolvedDisabled);
        }

        if (PressedChanged.HasDelegate)
        {
            await PressedChanged.InvokeAsync(nextPressed);
        }

        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
        StateHasChanged();
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (!IsInGroup || ResolvedDisabled || !GroupContext!.GroupElement.HasValue || !Element.HasValue)
        {
            return;
        }

        var orientation = GroupContext.Orientation;
        var isHorizontal = orientation == Orientation.Horizontal;
        var isVertical = orientation == Orientation.Vertical;

        var shouldNavigatePrevious =
            (isHorizontal && e.Key == "ArrowLeft") ||
            (isVertical && e.Key == "ArrowUp");

        var shouldNavigateNext =
            (isHorizontal && e.Key == "ArrowRight") ||
            (isVertical && e.Key == "ArrowDown");

        try
        {
            var module = await moduleTask.Value;
            if (shouldNavigatePrevious)
            {
                await module.InvokeVoidAsync("navigateToPrevious", GroupContext.GroupElement.Value, Element.Value);
            }
            else if (shouldNavigateNext)
            {
                await module.InvokeVoidAsync("navigateToNext", GroupContext.GroupElement.Value, Element.Value);
            }
            else if (e.Key == "Home")
            {
                await module.InvokeVoidAsync("navigateToFirst", GroupContext.GroupElement.Value);
            }
            else if (e.Key == "End")
            {
                await module.InvokeVoidAsync("navigateToLast", GroupContext.GroupElement.Value);
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }

        await EventUtilities.InvokeOnKeyDownAsync(AdditionalAttributes, e);
    }
}
