using System.Diagnostics.CodeAnalysis;
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
    public ElementReference? Element { get; private set; }

    private bool IsVertical => Context?.Orientation == Orientation.Vertical;

    private bool IsRtl => DirectionContext?.Direction == Direction.Rtl;

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
                attributes.TryGetValue("style", out var existingStyle) ? existingStyle.ToString() : null,
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
            attributes.TryGetValue("style", out var existingStyle) ? existingStyle.ToString() : null,
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

            var config = new SliderDragConfig(Context.Min, Context.Max, Context.Step, Context.MinStepsBetweenValues, Context.Orientation.ToDataAttributeString() ?? "horizontal", IsRtl ? "rtl" : "ltr", Context.ThumbCollisionBehavior.ToDataAttributeString(), Context.ThumbAlignment.ToDataAttributeString(), Context.Values, Context.Disabled, Context.ReadOnly, Context.ThumbAlignment == ThumbAlignment.Edge ? await GetInsetOffsetAsync() : 0);

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
