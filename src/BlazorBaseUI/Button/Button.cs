using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Button;

public class Button : ComponentBase, IReferencableComponent, IAsyncDisposable
{
    private const string DefaultTag = "button";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool hasRendered;
    private Func<bool, Task> cachedSyncJsCallback = default!;
    private bool previousDisabled;
    private bool previousFocusableWhenDisabled;
    private bool previousNativeButton;
    private bool isComponentRenderAs;
    private ButtonState state = new(false);

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private ILogger<Button> Logger { get; set; } = default!;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool FocusableWhenDisabled { get; set; }

    [Parameter]
    public bool NativeButton { get; set; } = true;

    [Parameter]
    public int TabIndex { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<ButtonState, string?>? ClassValue { get; set; }

    [Parameter]
    public Func<ButtonState, string?>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private bool NeedsJsInterop => !NativeButton || (Disabled && FocusableWhenDisabled);

    public Button()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/BlazorBaseUI/blazor-baseui-button.js").AsTask());
    }

    protected override void OnInitialized()
    {
        cachedSyncJsCallback = async (dispose) =>
        {
            try
            {
                await SyncJsAsync(dispose);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error syncing JS state in {Component}", nameof(Button));
            }
        };
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (state.Disabled != Disabled)
        {
            state = new ButtonState(Disabled);
        }

        if (!hasRendered)
        {
            return;
        }

        var stateChanged = Disabled != previousDisabled ||
                           FocusableWhenDisabled != previousFocusableWhenDisabled ||
                           NativeButton != previousNativeButton;

        if (!stateChanged)
        {
            return;
        }

        var previousNeedsJs = !previousNativeButton || (previousDisabled && previousFocusableWhenDisabled);
        var currentNeedsJs = NeedsJsInterop;

        previousDisabled = Disabled;
        previousFocusableWhenDisabled = FocusableWhenDisabled;
        previousNativeButton = NativeButton;

        if (currentNeedsJs)
        {
            _ = InvokeAsync(() => cachedSyncJsCallback(false));
        }
        else if (previousNeedsJs)
        {
            _ = InvokeAsync(() => cachedSyncJsCallback(true));
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);

        if (NativeButton)
        {
            builder.AddAttribute(2, "type", "button");
            if (Disabled && FocusableWhenDisabled)
            {
                builder.AddAttribute(3, "aria-disabled", "true");
                builder.AddAttribute(4, "tabindex", TabIndex);
            }
            else if (Disabled)
            {
                builder.AddAttribute(5, "disabled", true);
            }
            else
            {
                builder.AddAttribute(6, "tabindex", TabIndex);
            }
        }
        else
        {
            builder.AddAttribute(7, "role", "button");
            if (Disabled)
            {
                builder.AddAttribute(8, "aria-disabled", "true");
                builder.AddAttribute(9, "tabindex", FocusableWhenDisabled ? TabIndex : -1);
            }
            else
            {
                builder.AddAttribute(10, "tabindex", TabIndex);
            }
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(11, "class", resolvedClass);
        }
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(12, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddAttribute(13, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(14, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(15, elementReference => Element = elementReference);
            builder.AddContent(16, ChildContent);
            builder.CloseElement();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            previousDisabled = Disabled;
            previousFocusableWhenDisabled = FocusableWhenDisabled;
            previousNativeButton = NativeButton;

            if (NeedsJsInterop)
            {
                await SyncJsAsync(dispose: false);
            }
        }
    }

    private async Task SyncJsAsync(bool dispose)
    {
        if (!Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("sync", Element.Value, Disabled, FocusableWhenDisabled, NativeButton, dispose);
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
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("sync", Element.Value, false, false, false, true);
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }
}
