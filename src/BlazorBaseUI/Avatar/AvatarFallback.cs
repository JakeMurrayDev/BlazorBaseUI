using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;

namespace BlazorBaseUI.Avatar;

public sealed class AvatarFallback : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "span";

    private CancellationTokenSource? delayCts;
    private Func<Task> cachedStartDelayCallback = default!;

    private AvatarRootState state = new(ImageLoadingStatus.Idle);
    private int? previousDelay;
    private bool delayPassed;
    private bool isComponentRenderAs;

    [Inject]
    private ILogger<AvatarFallback> Logger { get; set; } = default!;

    [Inject]
    private TimeProvider TimeProvider { get; set; } = default!;

    [CascadingParameter]
    private AvatarRootContext? Context { get; set; }

    [Parameter]
    public int? Delay { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<AvatarRootState, string?>? ClassValue { get; set; }

    [Parameter]
    public Func<AvatarRootState, string?>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        if (Context is null)
        {
            throw new InvalidOperationException(
                "Base UI: AvatarRootContext is missing. Avatar parts must be placed within <AvatarRoot>.");
        }

        delayPassed = Delay is null;

        cachedStartDelayCallback = async () =>
        {
            try
            {
                await StartDelayAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error starting delay in {Component}", nameof(AvatarFallback));
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

        var currentStatus = Context?.ImageLoadingStatus ?? ImageLoadingStatus.Idle;
        if (state.ImageLoadingStatus != currentStatus)
        {
            state = new AvatarRootState(currentStatus);
        }

        if (Delay != previousDelay)
        {
            previousDelay = Delay;
            delayCts?.Cancel();

            if (Delay is null)
            {
                delayPassed = true;
            }
            else
            {
                delayPassed = false;
                _ = InvokeAsync(cachedStartDelayCallback);
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context?.ImageLoadingStatus == ImageLoadingStatus.Loaded || !delayPassed)
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
            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(2, "class", resolvedClass);
            }
            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(3, "style", resolvedStyle);
            }
            builder.AddAttribute(4, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(5, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(2, "class", resolvedClass);
            }
            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(3, "style", resolvedStyle);
            }
            builder.AddElementReferenceCapture(4, elementReference => Element = elementReference);
            builder.AddContent(5, ChildContent);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    private async Task StartDelayAsync()
    {
        delayCts?.Cancel();
        delayCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(Delay!.Value), TimeProvider, delayCts.Token);
            delayPassed = true;
            await InvokeAsync(StateHasChanged);
        }
        catch (TaskCanceledException)
        {
        }
    }

    public void Dispose()
    {
        delayCts?.Cancel();
        delayCts?.Dispose();
    }
}
