using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorBaseUI.DirectionProvider;

namespace BlazorBaseUI.Slider;

public sealed class SliderControl : ComponentBase, IReferencableComponent, IAsyncDisposable
{
    private const string DefaultTag = "div";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-slider.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool hasRendered;
    private bool isComponentRenderAs;
    private bool isProcessingPointerDown;
    private DotNetObjectReference<SliderControl>? dotNetRef;
    private ElementReference element;
    private SliderRootState state = SliderRootState.Default;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private ISliderRootContext? Context { get; set; }

    [CascadingParameter]
    private DirectionProviderContext? DirectionContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<SliderRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<SliderRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private bool IsVertical => Context?.Orientation == Orientation.Vertical;

    private bool IsRtl => DirectionContext?.Direction == Direction.Rtl;

    public SliderControl()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;

        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (Context is not null)
        {
            state = Context.State;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
            return;

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var orientationStr = state.Orientation.ToDataAttributeString() ?? "horizontal";

        var baseStyle = "touch-action: none;";
        var combinedStyle = string.IsNullOrEmpty(resolvedStyle) ? baseStyle : $"{resolvedStyle.TrimEnd().TrimEnd(';')}; {baseStyle}";

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "tabindex", -1);
        builder.AddAttribute(3, "onpointerdown", EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerDown));

        if (state.Dragging)
        {
            builder.AddAttribute(4, "data-dragging", string.Empty);
        }

        builder.AddAttribute(5, "data-orientation", orientationStr);

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

        if (state.Focused)
        {
            builder.AddAttribute(13, "data-focused", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(14, "class", resolvedClass);
        }

        builder.AddAttribute(15, "style", combinedStyle);

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(16, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(17, component =>
            {
                Element = ((IReferencableComponent)component).Element;
                if (Element.HasValue)
                {
                    element = Element.Value;
                    Context?.SetControlElement(element);
                }
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(18, e =>
            {
                element = e;
                Element = e;
                Context?.SetControlElement(e);
            });
            builder.AddContent(19, ChildContent);
            builder.CloseElement();
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            dotNetRef = DotNetObjectReference.Create(this);
        }
    }

    public async ValueTask DisposeAsync()
    {
        dotNetRef?.Dispose();

        if (moduleTask.IsValueCreated)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("stopDrag", element);
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    [JSInvokable]
    public void OnDragMove(double[] values, int thumbIndex)
    {
        if (Context is null)
            return;

        Context.SetValue(values, SliderChangeReason.Drag, thumbIndex);
    }

    [JSInvokable]
    public void OnDragEnd(double[] values, int thumbIndex)
    {
        if (Context is null)
            return;

        Context.SetValue(values, SliderChangeReason.Drag, thumbIndex);
        Context.CommitValue(values, SliderChangeReason.Drag);
        Context.SetDragging(false);
        Context.SetActiveThumbIndex(-1);
    }

    private async Task HandlePointerDown(PointerEventArgs e)
    {
        if (Context is null || Context.Disabled || Context.ReadOnly || e.Button != 0)
            return;

        if (isProcessingPointerDown)
            return;

        isProcessingPointerDown = true;

        try
        {
            if (!hasRendered)
                return;

            var thumbs = Context.GetAllThumbMetadata();
            var thumbElements = thumbs
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => kvp.Value.ThumbElement)
                .ToArray();

            var config = new SliderDragConfig(
                Context.Min,
                Context.Max,
                Context.Step,
                Context.MinStepsBetweenValues,
                Context.Orientation.ToDataAttributeString() ?? "horizontal",
                IsRtl ? "rtl" : "ltr",
                Context.ThumbCollisionBehavior.ToDataAttributeString(),
                Context.ThumbAlignment.ToDataAttributeString(),
                Context.Values,
                Context.Disabled,
                Context.ReadOnly,
                Context.ThumbAlignment == ThumbAlignment.Edge ? await GetInsetOffsetAsync() : 0,
                Context.HasRealtimeSubscribers);

            try
            {
                var module = await moduleTask.Value;

                await module.InvokeVoidAsync("setPointerCapture", element, e.PointerId);

                var result = await module.InvokeAsync<StartDragResult?>(
                    "startDrag",
                    element,
                    dotNetRef,
                    config,
                    thumbElements,
                    Context.GetIndicatorElement(),
                    e.ClientX,
                    e.ClientY);

                if (result is not null)
                {
                    Context.SetValueSilent(result.Values);
                    Context.SetDragging(true);
                    Context.SetActiveThumbIndex(result.ThumbIndex);

                    await FocusThumbAsync(result.ThumbIndex);
                }
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
        finally
        {
            isProcessingPointerDown = false;
        }
    }

    private async Task<double> GetInsetOffsetAsync()
    {
        if (!hasRendered || Context is null)
            return 0;

        var firstThumb = Context.GetAllThumbMetadata().OrderBy(kvp => kvp.Key).FirstOrDefault().Value;
        if (firstThumb is null)
            return 0;

        try
        {
            var module = await moduleTask.Value;
            var thumbRect = await module.InvokeAsync<ThumbRect?>("getThumbRect", firstThumb.ThumbElement);
            if (thumbRect is null)
                return 0;

            return IsVertical ? thumbRect.Height / 2 : thumbRect.Width / 2;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
            return 0;
        }
    }

    private async Task FocusThumbAsync(int index)
    {
        if (!hasRendered || Context is null)
            return;

        var thumbMeta = Context.GetThumbMetadata(index);
        if (thumbMeta is null)
            return;

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("focusThumbInput", thumbMeta.ThumbElement);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }
}
