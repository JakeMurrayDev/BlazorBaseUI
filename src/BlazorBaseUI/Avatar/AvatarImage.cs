using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace BlazorBaseUI.Avatar;

public sealed class AvatarImage : ComponentBase, IAsyncDisposable
{
    private const string DefaultTag = "img";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;
    private ImageLoadingStatus imageLoadingStatus = ImageLoadingStatus.Idle;
    private string? previousSrc;
    private bool hasRendered;
    private ElementReference element;

    [CascadingParameter]
    private AvatarRootContext? Context { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<AvatarRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<AvatarRootState, string>? StyleValue { get; set; }

    [Parameter]
    public EventCallback<ImageLoadingStatus> OnLoadingStatusChange { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull]
    public ElementReference? Element => element;

    private AvatarRootState State => new(imageLoadingStatus);

    private string? Src => AttributeUtilities.GetAttributeStringValue(AdditionalAttributes, "src");

    private string? ReferrerPolicy => AttributeUtilities.GetAttributeStringValue(AdditionalAttributes, "referrerpolicy");

    private string? CrossOrigin => AttributeUtilities.GetAttributeStringValue(AdditionalAttributes, "crossorigin");

    public AvatarImage()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/BlazorBaseUI/blazor-baseui-avatar-image.js").AsTask());
    }

    protected override void OnParametersSet()
    {
        if (hasRendered && Src != previousSrc)
        {
            previousSrc = Src;
            _ = LoadImageAsync();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (imageLoadingStatus != ImageLoadingStatus.Loaded)
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
            builder.CloseComponent();
        }
        else
        {
            var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
            builder.OpenElement(5, tag);
            builder.AddAttribute(6, "class", resolvedClass);
            builder.AddAttribute(7, "style", resolvedStyle);
            builder.AddMultipleAttributes(8, AdditionalAttributes);
            builder.AddElementReferenceCapture(9, elemRef => element = elemRef);
            builder.CloseElement();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            previousSrc = Src;
            await LoadImageAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
            catch (Exception ex) when (
                ex is JSDisconnectedException or
                TaskCanceledException)
            {
            }
        }
    }

    private async Task LoadImageAsync()
    {
        if (Context is null)
        {
            throw new InvalidOperationException(
                "Base UI: AvatarRootContext is missing. Avatar parts must be placed within <AvatarRoot>.");
        }

        try
        {
            var module = await moduleTask.Value;
            var status = await module.InvokeAsync<string>("loadImage", Src, ReferrerPolicy, CrossOrigin);

            imageLoadingStatus = status switch
            {
                "loaded" => ImageLoadingStatus.Loaded,
                "error" => ImageLoadingStatus.Error,
                _ => ImageLoadingStatus.Idle
            };

            await OnLoadingStatusChange.InvokeAsync(imageLoadingStatus);
            Context.SetImageLoadingStatus(imageLoadingStatus);
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex) when (
            ex is JSDisconnectedException or
            TaskCanceledException)
        {
        }
    }
}
