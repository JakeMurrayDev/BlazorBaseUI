using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorBaseUI.DirectionProvider;

namespace BlazorBaseUI.Slider;

public sealed class SliderControl : ComponentBase, IAsyncDisposable
{
    private const string DefaultTag = "div";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-slider.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool hasRendered;
    private bool isProcessingPointerDown;
    private DotNetObjectReference<SliderControl>? dotNetRef;
    private ElementReference element;

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

    [DisallowNull]
    public ElementReference? Element => element;

    private bool IsVertical => Context?.Orientation == Orientation.Vertical;

    private bool IsRtl => DirectionContext?.Direction == Direction.Rtl;

    private bool IsRange => Context?.Values.Length > 1;

    public SliderControl()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
            return;

        var state = Context.State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var attributes = BuildControlAttributes(state);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            attributes["style"] = CombineStyles(
                attributes.TryGetValue("style", out var existingStyle) ? existingStyle?.ToString() : null,
                resolvedStyle);
        }

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
        builder.AddElementReferenceCapture(5, e =>
        {
            element = e;
            Context?.SetControlElement(e);
        });
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
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
    public void OnDragEnd(double[] values, int thumbIndex)
    {
        if (Context is null)
            return;

        // Update values through the full SetValue path to trigger ValueChanged callbacks
        Context.SetValue(values, SliderChangeReason.Drag, thumbIndex);
        Context.CommitValue(values, SliderChangeReason.Drag);
        Context.SetDragging(false);
        Context.SetActiveThumbIndex(-1);
    }

    private Dictionary<string, object> BuildControlAttributes(SliderRootState state)
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

        attributes["tabindex"] = -1;
        attributes["style"] = CombineStyles(
            attributes.TryGetValue("style", out var existingStyle) ? existingStyle?.ToString() : null,
            "touch-action: none;");
        attributes["onpointerdown"] = EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerDown);

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
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

            var config = new SliderDragConfig
            {
                min = Context.Min,
                max = Context.Max,
                step = Context.Step,
                minStepsBetweenValues = Context.MinStepsBetweenValues,
                orientation = Context.Orientation.ToDataAttributeString() ?? "horizontal",
                direction = IsRtl ? "rtl" : "ltr",
                collisionBehavior = Context.ThumbCollisionBehavior.ToDataAttributeString() ?? "push",
                thumbAlignment = Context.ThumbAlignment.ToDataAttributeString() ?? "center",
                values = Context.Values,
                disabled = Context.Disabled,
                readOnly = Context.ReadOnly,
                insetOffset = Context.ThumbAlignment == ThumbAlignment.Edge ? await GetInsetOffsetAsync() : 0
            };

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
                    // Sync values and set dragging state without triggering re-renders
                    // JS controls the DOM directly during drag - Blazor re-renders would
                    // overwrite JS DOM updates and cause visible delays
                    Context.SetValueSilent(result.values);
                    Context.SetDragging(true);  // Set dragging first so SetActiveThumbIndex won't re-render
                    Context.SetActiveThumbIndex(result.thumbIndex);

                    await FocusThumbAsync(result.thumbIndex);
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

            return IsVertical ? thumbRect.height / 2 : thumbRect.width / 2;
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

    private sealed class ThumbRect
    {
        public double left { get; set; }
        public double right { get; set; }
        public double top { get; set; }
        public double bottom { get; set; }
        public double width { get; set; }
        public double height { get; set; }
        public double midX { get; set; }
        public double midY { get; set; }
    }

    private sealed class StartDragResult
    {
        public int thumbIndex { get; set; }
        public double[] values { get; set; } = [];
    }

    private sealed class SliderDragConfig
    {
        public double min { get; init; }
        public double max { get; init; }
        public double step { get; init; }
        public int minStepsBetweenValues { get; init; }
        public string orientation { get; init; } = "horizontal";
        public string direction { get; init; } = "ltr";
        public string collisionBehavior { get; init; } = "push";
        public string thumbAlignment { get; init; } = "center";
        public double[] values { get; init; } = [];
        public bool disabled { get; init; }
        public bool readOnly { get; init; }
        public double insetOffset { get; init; }
    }

    private static string CombineStyles(string? existing, string additional)
    {
        if (string.IsNullOrEmpty(existing))
            return additional;

        var trimmed = existing.TrimEnd();
        if (!trimmed.EndsWith(';'))
            trimmed += ";";

        return trimmed + " " + additional;
    }
}
