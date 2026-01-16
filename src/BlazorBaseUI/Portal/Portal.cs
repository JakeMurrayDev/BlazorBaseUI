using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Portal;

public sealed class Portal : ComponentBase, IReferencableComponent, IAsyncDisposable
{
    private const string DefaultTag = "div";

    private readonly string defaultId = Guid.NewGuid().ToIdString();

    private Lazy<Task<IJSObjectReference>>? moduleTask;
    private bool hasRendered;
    private bool disposed;
    private bool isComponentRenderAs;
    private IReferencableComponent? componentReference;

    private Lazy<Task<IJSObjectReference>> ModuleTask => moduleTask ??= new Lazy<Task<IJSObjectReference>>(() =>
        JSRuntime!.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlazorBaseUI/blazor-baseui-portal.js").AsTask());

    [Inject]
    private IJSRuntime? JSRuntime { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public string Target { get; set; } = "body";

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private string Id => GetIdOrDefault(AdditionalAttributes, defaultId);

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !disposed)
        {
            try
            {
                var module = await ModuleTask.Value;
                await module.InvokeVoidAsync("createPortal", Id, Target);
                hasRendered = true;
                StateHasChanged();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var style = AttributeUtilities.CombineStyles(AdditionalAttributes, !hasRendered ? "display: none;" : null);

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "data-blazor-base-ui-portal", string.Empty);
            builder.AddAttribute(3, "id", Id);

            if (!string.IsNullOrEmpty(style))
            {
                builder.AddAttribute(4, "style", style);
            }

            builder.AddAttribute(5, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(6, component =>
            {
                componentReference = (IReferencableComponent)component;
                Element = componentReference.Element;
            });
            builder.CloseComponent();
        }
        else
        {
            builder.OpenElement(7, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(8, AdditionalAttributes);
            builder.AddAttribute(9, "data-blazor-base-ui-portal", string.Empty);
            builder.AddAttribute(10, "id", Id);

            if (!string.IsNullOrEmpty(style))
            {
                builder.AddAttribute(11, "style", style);
            }

            builder.AddContent(12, ChildContent);
            builder.AddElementReferenceCapture(13, elementReference => Element = elementReference);
            builder.CloseElement();
        }
    }

    private static string GetIdOrDefault(IReadOnlyDictionary<string, object>? attributes, string defaultValue)
    {
        if (attributes is not null && attributes.TryGetValue("id", out var id) && id is string idString)
        {
            return idString;
        }

        return defaultValue;
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (moduleTask?.IsValueCreated == true)
        {
            try
            {
                var module = await ModuleTask.Value;
                await module.InvokeVoidAsync("restorePortal", Id);
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }
}
