using BlazorBaseUI.Utilities.LabelableProvider;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace BlazorBaseUI.Field;

public sealed class FieldLabel : ComponentBase, IFieldStateSubscriber, IAsyncDisposable
{
    private const string DefaultTag = "label";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-label.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private string? defaultId;
    private string labelId = null!;
    private Dictionary<string, object>? cachedAttributes;
    private FieldRootState lastAttributeState;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    [CascadingParameter]
    private LabelableContext? LabelableContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<FieldRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<FieldRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private FieldRootState State => FieldContext?.State ?? FieldRootState.Default;

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    public FieldLabel()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        labelId = ResolvedId;
        LabelableContext?.SetLabelId(labelId);
        FieldContext?.Subscribe(this);
    }

    protected override void OnParametersSet()
    {
        cachedAttributes = null;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var state = State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        var attributes = GetOrBuildAttributes(state);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        else
            attributes.Remove("class");

        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;
        else
            attributes.Remove("style");

        if (RenderAs is not null)
        {
            if (!typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
            {
                throw new InvalidOperationException($"Type {RenderAs.Name} must implement IReferencableComponent.");
            }
            builder.OpenComponent(0, RenderAs);
            builder.AddMultipleAttributes(1, attributes);
            builder.AddComponentParameter(2, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(3, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
            return;
        }

        var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
        builder.OpenElement(3, tag);
        builder.AddMultipleAttributes(4, attributes);
        builder.AddElementReferenceCapture(5, e => Element = e);
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                var module = await moduleTask.Value;

                if (Element.HasValue)
                {
                    await module.InvokeVoidAsync("addLabelMouseDownListener", Element.Value);
                }
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    void IFieldStateSubscriber.NotifyStateChanged()
    {
        cachedAttributes = null;
        _ = InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        LabelableContext?.SetLabelId(null);
        FieldContext?.Unsubscribe(this);

        if (moduleTask.IsValueCreated && Element.HasValue)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("removeLabelMouseDownListener", Element.Value);
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    private Dictionary<string, object> GetOrBuildAttributes(FieldRootState state)
    {
        if (cachedAttributes is not null && lastAttributeState == state)
            return cachedAttributes;

        cachedAttributes = BuildAttributes(state);
        lastAttributeState = state;
        return cachedAttributes;
    }

    private Dictionary<string, object> BuildAttributes(FieldRootState state)
    {
        var dataAttrs = state.GetDataAttributes();
        var additionalCount = AdditionalAttributes?.Count ?? 0;
        var attributes = new Dictionary<string, object>(dataAttrs.Count + additionalCount + 2);

        if (AdditionalAttributes is not null)
        {
            foreach (var attr in AdditionalAttributes)
            {
                if (attr.Key is not "class" and not "style")
                    attributes[attr.Key] = attr.Value;
            }
        }

        attributes["id"] = labelId;

        if (!string.IsNullOrEmpty(LabelableContext?.ControlId))
            attributes["for"] = LabelableContext.ControlId;

        foreach (var dataAttr in dataAttrs)
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }
}
