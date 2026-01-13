using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Avatar;

public sealed class AvatarImage : ComponentBase, IReferencableComponent, IAsyncDisposable
{
    private const string DefaultTag = "img";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private ImageLoadingStatus imageLoadingStatus = ImageLoadingStatus.Idle;
    private Func<Task> cachedLoadImageCallback = default!;
    private AvatarRootState state = new(ImageLoadingStatus.Idle);
    private string? previousSrc;
    private bool hasRendered;
    private bool isComponentRenderAs;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private ILogger<AvatarImage> Logger { get; set; } = default!;

    [CascadingParameter]
    private AvatarRootContext? Context { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<AvatarRootState, string?>? ClassValue { get; set; }

    [Parameter]
    public Func<AvatarRootState, string?>? StyleValue { get; set; }

    [Parameter]
    public EventCallback<ImageLoadingStatus> OnLoadingStatusChange { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

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

    protected override void OnInitialized()
    {
        cachedLoadImageCallback = async () =>
        {
            try
            {
                await LoadImageAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading image in {Component}", nameof(AvatarImage));
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

        if (state.ImageLoadingStatus != imageLoadingStatus)
        {
            state = new AvatarRootState(imageLoadingStatus);
        }

        if (hasRendered && Src != previousSrc)
        {
            previousSrc = Src;
            _ = InvokeAsync(cachedLoadImageCallback);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (imageLoadingStatus != ImageLoadingStatus.Loaded)
        {
            return;
        }

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

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(2, "class", resolvedClass);
        }
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(3, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddAttribute(4, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(6, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(7, elementReference => Element = elementReference);
            builder.AddContent(8, ChildContent);
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
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
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

            state = new AvatarRootState(imageLoadingStatus);
            await OnLoadingStatusChange.InvokeAsync(imageLoadingStatus);
            Context.SetImageLoadingStatus(imageLoadingStatus);
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }
}
