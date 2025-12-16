using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Diagnostics.CodeAnalysis;

namespace BlazorBaseUI.Avatar;

public sealed class AvatarFallback : ComponentBase
{
    private const string DefaultTag = "span";

    private CancellationTokenSource? delayCts;
    private bool delayPassed;
    private ElementReference element;

    [CascadingParameter]
    private AvatarRootContext? Context { get; set; }

    [Parameter]
    public int? Delay { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<AvatarRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<AvatarRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull]
    public ElementReference? Element => element;

    private AvatarRootState State => Context?.State ?? new(ImageLoadingStatus.Idle);

    protected override void OnInitialized()
    {
        if (Context is null)
        {
            throw new InvalidOperationException(
                "Base UI: AvatarRootContext is missing. Avatar parts must be placed within <AvatarRoot>.");
        }

        delayPassed = Delay is null;
    }

    protected override void OnParametersSet()
    {
        if (Delay is not null && !delayPassed)
        {
            _ = StartDelayAsync();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context?.ImageLoadingStatus == ImageLoadingStatus.Loaded || !delayPassed)
        {
            return;
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(State));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(State));

        if (RenderAs is not null)
        {
            builder.OpenComponent(1, RenderAs);
            builder.AddAttribute(2, "class", resolvedClass);
            builder.AddAttribute(3, "style", resolvedStyle);
            builder.AddMultipleAttributes(4, AdditionalAttributes);
            builder.AddAttribute(5, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(6, obj => { });
            builder.CloseComponent();
        }
        else
        {
            var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
            builder.OpenElement(7, tag);
            builder.AddAttribute(8, "class", resolvedClass);
            builder.AddAttribute(9, "style", resolvedStyle);
            builder.AddMultipleAttributes(10, AdditionalAttributes);
            builder.AddElementReferenceCapture(11, elemRef => element = elemRef);
            builder.AddContent(12, ChildContent);
            builder.CloseElement();
        }
    }

    private async Task StartDelayAsync()
    {
        delayCts?.Cancel();
        delayCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(Delay!.Value, delayCts.Token);
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
