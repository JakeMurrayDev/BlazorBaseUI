using BlazorBaseUI.Utilities.LabelableProvider;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Field;

public sealed class FieldLabel : ComponentBase, IFieldStateSubscriber, IAsyncDisposable
{
    private const string DefaultTag = "label";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-label.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private string? defaultId;
    private string labelId = null!;

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

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var state = State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var isComponent = RenderAs is not null;

        if (isComponent)
        {
            if (!typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
            {
                throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
            }
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "id", labelId);

        if (!string.IsNullOrEmpty(LabelableContext?.ControlId))
        {
            builder.AddAttribute(3, "for", LabelableContext.ControlId);
        }

        if (state.Disabled)
        {
            builder.AddAttribute(4, "data-disabled", string.Empty);
        }

        if (state.Valid == true)
        {
            builder.AddAttribute(5, "data-valid", string.Empty);
        }
        else if (state.Valid == false)
        {
            builder.AddAttribute(6, "data-invalid", string.Empty);
        }

        if (state.Touched)
        {
            builder.AddAttribute(7, "data-touched", string.Empty);
        }

        if (state.Dirty)
        {
            builder.AddAttribute(8, "data-dirty", string.Empty);
        }

        if (state.Filled)
        {
            builder.AddAttribute(9, "data-filled", string.Empty);
        }

        if (state.Focused)
        {
            builder.AddAttribute(10, "data-focused", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(11, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(12, "style", resolvedStyle);
        }

        if (isComponent)
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
}
