using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorBaseUI.NumberField;

public sealed class NumberFieldScrubArea : ComponentBase, IReferencableComponent, IAsyncDisposable
{
    private const string DefaultTag = "span";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-number-field.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool hasRendered;
    private bool isComponentRenderAs;
    private bool isScrubbing;
    private bool isTouchInput;
    private bool isPointerLockDenied;
    private ElementReference? scrubAreaElement;
    private ElementReference? cursorElement;
    private DotNetObjectReference<NumberFieldScrubArea>? dotNetRef;
    private NumberFieldScrubAreaContext scrubContext = null!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private INumberFieldRootContext? RootContext { get; set; }

    [Parameter]
    public ScrubDirection Direction { get; set; } = ScrubDirection.Horizontal;

    [Parameter]
    public int PixelSensitivity { get; set; } = 2;

    [Parameter]
    public int? TeleportDistance { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<NumberFieldRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<NumberFieldRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private NumberFieldRootState State => RootContext?.State ?? NumberFieldRootState.Default;

    public NumberFieldScrubArea()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        scrubContext = new NumberFieldScrubAreaContext(
            setCursorElement: SetCursorElement,
            getCursorElement: () => cursorElement,
            setScrubAreaElement: SetScrubAreaElement,
            getScrubAreaElement: () => scrubAreaElement);

        UpdateScrubContext();
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;

        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        UpdateScrubContext();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            dotNetRef = DotNetObjectReference.Create(this);

            if (scrubAreaElement.HasValue)
            {
                try
                {
                    var module = await moduleTask.Value;
                    var config = new
                    {
                        direction = Direction.ToDataAttributeString(),
                        pixelSensitivity = PixelSensitivity,
                        teleportDistance = TeleportDistance
                    };
                    await module.InvokeVoidAsync("initializeScrubArea", scrubAreaElement.Value, dotNetRef, config);
                }
                catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
                {
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated && scrubAreaElement.HasValue)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("disposeScrubArea", scrubAreaElement.Value);
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }

        dotNetRef?.Dispose();
    }

    [JSInvokable]
    public void OnScrubMove(int direction, bool altKey, bool shiftKey)
    {
        if (RootContext?.Disabled == true || RootContext?.ReadOnly == true)
            return;

        var amount = RootContext?.GetStepAmount(altKey, shiftKey) ?? 1;
        RootContext?.IncrementValue(amount, direction, NumberFieldChangeReason.Scrub);
    }

    [JSInvokable]
    public void OnScrubEnd()
    {
        isScrubbing = false;
        RootContext?.SetIsScrubbing(false);
        RootContext?.OnValueCommitted(RootContext?.Value, NumberFieldChangeReason.Scrub);
        UpdateScrubContext();
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<INumberFieldScrubAreaContext>>(0);
        builder.AddComponentParameter(1, "Value", scrubContext);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(innerBuilder => BuildInnerContent(innerBuilder)));
        builder.CloseComponent();
    }

    private void BuildInnerContent(RenderTreeBuilder builder)
    {
        var state = State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var baseStyle = "touch-action:none;user-select:none;-webkit-user-select:none;";

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "role", "presentation");
        builder.AddAttribute(3, "style", baseStyle + (resolvedStyle ?? string.Empty));

        builder.AddAttribute(4, "onpointerdown", EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerDown));

        if (state.Scrubbing)
        {
            builder.AddAttribute(5, "data-scrubbing", string.Empty);
        }

        if (state.Disabled)
        {
            builder.AddAttribute(6, "data-disabled", string.Empty);
        }

        if (state.ReadOnly)
        {
            builder.AddAttribute(7, "data-readonly", string.Empty);
        }

        if (state.Required)
        {
            builder.AddAttribute(8, "data-required", string.Empty);
        }

        if (state.Valid == true)
        {
            builder.AddAttribute(9, "data-valid", string.Empty);
        }
        else if (state.Valid == false)
        {
            builder.AddAttribute(10, "data-invalid", string.Empty);
        }

        if (state.Touched)
        {
            builder.AddAttribute(11, "data-touched", string.Empty);
        }

        if (state.Dirty)
        {
            builder.AddAttribute(12, "data-dirty", string.Empty);
        }

        if (state.Filled)
        {
            builder.AddAttribute(13, "data-filled", string.Empty);
        }

        if (state.Focused)
        {
            builder.AddAttribute(14, "data-focused", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(15, "class", resolvedClass);
        }

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(16, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(17, component =>
            {
                Element = ((IReferencableComponent)component).Element;
                scrubAreaElement = Element;
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(18, elementReference =>
            {
                Element = elementReference;
                scrubAreaElement = elementReference;
            });
            builder.AddContent(19, ChildContent);
            builder.CloseElement();
        }
    }

    private async void HandlePointerDown(PointerEventArgs e)
    {
        if (RootContext?.Disabled == true || RootContext?.ReadOnly == true)
            return;

        if (e.Button != 0)
            return;

        var isTouch = e.PointerType == "touch";
        isTouchInput = isTouch;

        if (e.PointerType == "mouse")
        {
            RootContext?.FocusInput();
        }

        isScrubbing = true;
        RootContext?.SetIsScrubbing(true);
        UpdateScrubContext();
        StateHasChanged();

        if (!hasRendered || !scrubAreaElement.HasValue || dotNetRef is null)
            return;

        try
        {
            var module = await moduleTask.Value;
            var config = new
            {
                direction = Direction.ToDataAttributeString(),
                pixelSensitivity = PixelSensitivity,
                teleportDistance = TeleportDistance
            };

            var result = await module.InvokeAsync<ScrubStartResult>("startScrub",
                scrubAreaElement.Value,
                dotNetRef,
                cursorElement,
                config,
                e.ClientX,
                e.ClientY,
                isTouch);

            if (result.PointerLockDenied)
            {
                isPointerLockDenied = true;
                UpdateScrubContext();
                StateHasChanged();
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private void SetCursorElement(ElementReference? element)
    {
        cursorElement = element;
    }

    private void SetScrubAreaElement(ElementReference? element)
    {
        scrubAreaElement = element;
    }

    private void UpdateScrubContext()
    {
        scrubContext.Update(
            isScrubbing: isScrubbing,
            isTouchInput: isTouchInput,
            isPointerLockDenied: isPointerLockDenied,
            direction: Direction,
            pixelSensitivity: PixelSensitivity,
            teleportDistance: TeleportDistance);
    }

    private sealed class ScrubStartResult
    {
        public bool Success { get; set; }
        public bool PointerLockDenied { get; set; }
    }
}
